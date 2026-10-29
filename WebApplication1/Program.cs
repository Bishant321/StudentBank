using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<OpenBankingService>();
builder.Services.AddSingleton<MarketDataService>();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("assistant", limiter =>
    {
        limiter.PermitLimit = 12;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.AddFixedWindowLimiter("market-quotes", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=studentbank.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<BankDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "StudentBank.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/index.html";
    options.AccessDeniedPath = "/index.html";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<BankingService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();
    await using (var command = connection.CreateCommand())
    {
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'AspNetUsers'";
        var existingIdentitySchema = Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
        if (existingIdentitySchema)
        {
            command.CommandText = "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL)";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory";
            var hasMigrationHistory = Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
            if (!hasMigrationHistory)
            {
                command.CommandText = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261015110000_InitialBankingSchema', '10.0.0')";
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    await connection.CloseAsync();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole is null)
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }
    }

    var adminEmail = builder.Configuration["StudentBank:AdminEmail"];
    var adminPassword = builder.Configuration["StudentBank:AdminPassword"];
    if (!string.IsNullOrWhiteSpace(adminEmail) || !string.IsNullOrWhiteSpace(adminPassword))
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Configure both StudentBank:AdminEmail and StudentBank:AdminPassword to provision the administrator.");
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTime.UtcNow,
    database = "sqlite"
}));

app.MapPost("/api/assistant/chat", async Task<IResult> (
    AssistantChatRequest request,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var message = request.Message?.Trim();
    if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
    {
        return Results.BadRequest(new { message = "Write a question of up to 1,000 characters." });
    }

    var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["OPENAI_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.Problem(
            title: "AI assistant is not configured",
            detail: "Set the OPENAI_API_KEY environment variable on the StudentBank server to enable AI replies.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var payload = new
    {
        model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini",
        instructions = "You are the StudentBank learning assistant. Give concise, general educational information about budgeting and everyday banking. You are not a financial adviser. Never ask for passwords, PINs, card numbers, or one-time codes. Do not claim to see account data or execute payments, transfers, or other banking actions. For fraud, lost cards, or account access problems, direct the user to contact their bank through its official app or phone number.",
        input = message,
        max_output_tokens = 300
    };

    try
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient().SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI assistant provider returned HTTP {StatusCode}.", (int)response.StatusCode);
            return Results.Problem("The AI assistant could not answer right now. Please try again shortly.", statusCode: StatusCodes.Status502BadGateway);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await System.Text.Json.JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var reply = document.RootElement.GetProperty("output")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text")
            .Select(item => item.GetProperty("text").GetString())
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (string.IsNullOrWhiteSpace(reply))
        {
            return Results.Problem("The AI assistant returned an empty reply. Please try again.", statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new { reply });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        return Results.Problem("The request was cancelled.", statusCode: StatusCodes.Status408RequestTimeout);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "AI assistant request failed.");
        return Results.Problem("The AI assistant could not answer right now. Please try again shortly.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireRateLimiting("assistant");

var openBankingApi = app.MapGroup("/api/open-banking").RequireAuthorization();
openBankingApi.MapGet("/connect", (OpenBankingService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
    try
    {
        var start = service.CreateAuthorizationUrl(userId);
        return Results.Ok(start);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
openBankingApi.MapPost("/exchange", async (OpenBankingExchangeRequest request, OpenBankingService service, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.State) || string.IsNullOrWhiteSpace(request.Code))
        return Results.BadRequest(new { message = "Copy both the authorization code and state returned by the bank provider." });
    try
    {
        await service.ExchangeCodeAsync(userId, request.State, request.Code, cancellationToken);
        return Results.Ok(new { message = "Sandbox bank connection established for this server session." });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
});
openBankingApi.MapGet("/accounts", async (OpenBankingService service, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
    try
    {
        return Results.Ok(await service.GetAccountsAsync(userId, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("The external bank sandbox is unavailable. Try again later.", statusCode: StatusCodes.Status502BadGateway);
    }
});
openBankingApi.MapDelete("/connection", (OpenBankingService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
    service.Disconnect(userId);
    return Results.Ok(new { message = "The in-memory sandbox connection was removed." });
});

var marketApi = app.MapGroup("/api/market").RequireAuthorization();
marketApi.MapGet("/quote/{symbol}", async (string symbol, MarketDataService service, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.GetQuoteAsync(symbol, cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("The market data provider could not be reached. Try again later.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireRateLimiting("market-quotes");

var assistantApi = app.MapGroup("/api/assistant").RequireAuthorization();
assistantApi.MapPost("/spending-insights", async Task<IResult> (
    SpendingInsightsRequest request,
    OpenBankingService openBankingService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Program> logger,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
    if (!request.ConsentToShareTransactions)
        return Results.BadRequest(new { message = "Review and accept the transaction-data notice before requesting AI spending insights." });
    var question = request.Message?.Trim();
    if (string.IsNullOrWhiteSpace(question) || question.Length > 1000)
        return Results.BadRequest(new { message = "Write a question of up to 1,000 characters." });
    var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["OPENAI_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem("Set OPENAI_API_KEY on the StudentBank server to enable AI replies.", statusCode: StatusCodes.Status503ServiceUnavailable);

    try
    {
        var accounts = await openBankingService.GetAccountsAsync(userId, cancellationToken);
        var categories = accounts.SelectMany(account => account.Transactions)
            .Where(transaction => transaction.Amount < 0m)
            .GroupBy(transaction => new { transaction.Currency, transaction.Category })
            .Select(group => new { group.Key.Currency, category = group.Key.Category, transactions = group.Count(), total = Math.Abs(group.Sum(item => item.Amount)) })
            .OrderByDescending(group => group.total)
            .ToArray();
        if (categories.Length == 0)
            return Results.Ok(new { reply = "I could not find recent outgoing transactions in the connected sandbox accounts." });

        var payload = new
        {
            model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini",
            instructions = "You are an educational student budgeting assistant. The user explicitly chose to share their 30-day category totals for this request. Explain only descriptive patterns in the supplied aggregated transaction categories. Do not identify individual purchases, infer sensitive traits, recommend financial products, or give investment, debt, or personalized financial advice. Do not claim to make payments or access any data beyond the aggregates shown. Mention that provider categories may be approximate and this is not financial advice.",
            input = $"Question: {question}\n30-day outgoing transaction aggregates by currency and category (no merchant names or account identifiers): {System.Text.Json.JsonSerializer.Serialize(categories)}",
            max_output_tokens = 350
        };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient().SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI spending insight provider returned HTTP {StatusCode}.", (int)response.StatusCode);
            return Results.Problem("The assistant could not create an insight right now. Try again later.", statusCode: StatusCodes.Status502BadGateway);
        }
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var reply = document.RootElement.GetProperty("output").EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text")
            .Select(item => item.GetProperty("text").GetString())
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        return string.IsNullOrWhiteSpace(reply)
            ? Results.Problem("The assistant returned an empty reply.", statusCode: StatusCodes.Status502BadGateway)
            : Results.Ok(new { reply });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Could not load the connected bank data or reach the AI provider.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireRateLimiting("assistant");

assistantApi.MapPost("/market-explanation", async Task<IResult> (
    MarketExplanationRequest request,
    MarketDataService marketDataService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var question = request.Message?.Trim();
    if (string.IsNullOrWhiteSpace(question) || question.Length > 500)
        return Results.BadRequest(new { message = "Write a short question about the displayed quote." });
    var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["OPENAI_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem("Set OPENAI_API_KEY on the StudentBank server to enable AI replies.", statusCode: StatusCodes.Status503ServiceUnavailable);
    try
    {
        var quote = await marketDataService.GetQuoteAsync(request.Symbol, cancellationToken);
        var payload = new
        {
            model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini",
            instructions = "Explain public market quote fields in plain educational language. Do not recommend buying, selling, or holding securities, predict returns, or give personalized investment advice. Explain that a quoted price can be delayed, may not be executable, and can change. Use only the quote supplied.",
            input = $"Question: {question}\nMarket quote: symbol {quote.Symbol}; price {quote.Price}; change {quote.Change} ({quote.ChangePercent}); trading day {quote.TradingDay}. Provider notice: {quote.DataNotice}",
            max_output_tokens = 250
        };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient().SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI market explanation provider returned HTTP {StatusCode}.", (int)response.StatusCode);
            return Results.Problem("The assistant could not explain this quote right now.", statusCode: StatusCodes.Status502BadGateway);
        }
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var reply = document.RootElement.GetProperty("output").EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text")
            .Select(item => item.GetProperty("text").GetString())
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        return string.IsNullOrWhiteSpace(reply) ? Results.Problem("The assistant returned an empty reply.", statusCode: StatusCodes.Status502BadGateway) : Results.Ok(new { reply });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("The quote or assistant provider could not be reached.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireRateLimiting("assistant");

var authApi = app.MapGroup("/api/auth");
authApi.MapPost("/register", async (RegisterRequest request, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName))
    {
        return Results.BadRequest(new { message = "Full name is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email and password are required." });
    }

    var newUser = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FullName = request.FullName.Trim()
    };

    var result = await userManager.CreateAsync(newUser, request.Password);
    if (!result.Succeeded)
    {
        return Results.BadRequest(new { message = string.Join("; ", result.Errors.Select(error => error.Description)) });
    }

    await signInManager.SignInAsync(newUser, isPersistent: true);
    return Results.Ok(new
    {
        message = "User registered successfully.",
        user = new { newUser.Id, newUser.Email, newUser.FullName, roles = new[] { "User" } }
    });
});

authApi.MapPost("/login", async (LoginRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email and password are required." });
    }

    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
    if (!passwordValid)
    {
        return Results.Unauthorized();
    }

    await signInManager.SignInAsync(user, isPersistent: true);
    var roles = await userManager.GetRolesAsync(user);

    return Results.Ok(new
    {
        message = "Login successful.",
        user = new { user.Id, user.Email, user.FullName, roles }
    });
});

authApi.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { message = "Logged out successfully." });
});

var protectedApi = app.MapGroup("/api").RequireAuthorization();

protectedApi.MapGet("/me", async (UserManager<ApplicationUser> userManager, ClaimsPrincipal user) =>
{
    var applicationUser = await userManager.GetUserAsync(user);
    if (applicationUser is null)
    {
        return Results.Unauthorized();
    }

    var roles = await userManager.GetRolesAsync(applicationUser);
    return Results.Ok(new
    {
        id = applicationUser.Id,
        email = applicationUser.Email,
        fullName = applicationUser.FullName,
        roles
    });
});

protectedApi.MapGet("/accounts", async (BankingService bankingService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var accounts = await bankingService.GetAccountsForUserAsync(userId);
    return Results.Ok(accounts.Select(account => new AccountSummaryDto(
        account.AccountNumber, account.HolderName, account.Balance, account.Currency, account.IsActive, account.CreatedAt)));
});

protectedApi.MapPost("/accounts", async (CreateAccountRequest request, BankingService bankingService, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(request.HolderName))
    {
        return Results.BadRequest(new { message = "Holder name is required." });
    }

    if (request.InitialDeposit < 0m)
    {
        return Results.BadRequest(new { message = "Initial deposit cannot be negative." });
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var account = await bankingService.CreateAccountAsync(userId, request.HolderName, request.InitialDeposit, request.Currency ?? "USD");
        return Results.Created($"/api/accounts/{account.AccountNumber}", account);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

protectedApi.MapGet("/accounts/{accountNumber}", async (string accountNumber, BankingService bankingService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var account = await bankingService.GetAccountForUserAsync(userId, accountNumber);
    return account is null ? Results.NotFound() : Results.Ok(account);
});

protectedApi.MapGet("/accounts/{accountNumber}/statement", async (
    string accountNumber,
    DateTime? from,
    DateTime? to,
    BankingService bankingService,
    ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    if (from > to)
    {
        return Results.BadRequest(new { message = "The start date must be before the end date." });
    }

    var statement = await bankingService.GetStatementAsync(userId, accountNumber, from, to);
    return statement is null ? Results.NotFound() : Results.Ok(statement);
});

protectedApi.MapPost("/accounts/{accountNumber}/deposit", async (string accountNumber, DepositRequest request, BankingService bankingService, ClaimsPrincipal user) =>
{
    if (request.Amount <= 0m)
    {
        return Results.BadRequest(new { message = "Deposit amount must be greater than zero." });
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var account = await bankingService.DepositAsync(userId, accountNumber, request.Amount, request.Description ?? "Deposit");
        return Results.Ok(account);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

protectedApi.MapPost("/accounts/{accountNumber}/withdraw", async (string accountNumber, WithdrawRequest request, BankingService bankingService, ClaimsPrincipal user) =>
{
    if (request.Amount <= 0m)
    {
        return Results.BadRequest(new { message = "Withdrawal amount must be greater than zero." });
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var account = await bankingService.WithdrawAsync(userId, accountNumber, request.Amount, request.Description ?? "Withdrawal");
        return Results.Ok(account);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

protectedApi.MapPost("/transfer", async (TransferRequest request, BankingService bankingService, ClaimsPrincipal user) =>
{
    if (request.Amount <= 0m)
    {
        return Results.BadRequest(new { message = "Transfer amount must be greater than zero." });
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var result = await bankingService.TransferAsync(userId, request.FromAccountNumber, request.ToAccountNumber, request.Amount, request.Description ?? "Transfer");
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

var adminApi = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly");
adminApi.MapGet("/summary", async (BankingService bankingService) =>
{
    var summary = await bankingService.GetAdminSummaryAsync();
    return Results.Ok(summary);
});

adminApi.MapGet("/accounts", async (BankingService bankingService) =>
    Results.Ok(await bankingService.GetAllAccountsAsync()));

app.Run();

public sealed class BankingService
{
    private readonly BankDbContext _dbContext;

    public BankingService(BankDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<BankAccount>> GetAccountsForUserAsync(string userId)
    {
        return await _dbContext.BankAccounts
            .Include(account => account.Transactions.OrderBy(transaction => transaction.Timestamp))
            .Where(account => account.UserId == userId && account.IsActive)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync();
    }

    public async Task<BankAccount?> GetAccountForUserAsync(string userId, string accountNumber)
    {
        return await _dbContext.BankAccounts
            .Include(account => account.Transactions.OrderBy(transaction => transaction.Timestamp))
            .FirstOrDefaultAsync(account => account.UserId == userId && account.AccountNumber == accountNumber);
    }

    public async Task<List<BankAccount>> GetAllAccountsAsync()
    {
        return await _dbContext.BankAccounts
            .Include(account => account.Transactions.OrderBy(transaction => transaction.Timestamp))
            .OrderBy(account => account.CreatedAt)
            .ToListAsync();
    }

    public async Task<AccountStatement?> GetStatementAsync(string userId, string accountNumber, DateTime? from, DateTime? to)
    {
        var account = await _dbContext.BankAccounts
            .AsNoTracking()
            .Include(item => item.Transactions.OrderBy(transaction => transaction.Timestamp))
            .Where(item => item.UserId == userId && item.AccountNumber == accountNumber)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return null;
        }

        var filteredTransactions = account.Transactions
            .Where(transaction => (!from.HasValue || transaction.Timestamp >= from.Value) &&
                                  (!to.HasValue || transaction.Timestamp < to.Value.Date.AddDays(1)))
            .Select(transaction => new StatementTransaction
            {
                Type = transaction.Type,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Timestamp = transaction.Timestamp,
                RelatedAccountNumber = transaction.RelatedAccountNumber
            })
            .ToList();

        var signedAmount = (Func<StatementTransaction, decimal>)(transaction =>
            transaction.Type is "deposit" or "transfer_in" ? transaction.Amount : -transaction.Amount);
        var allTransactions = account.Transactions.Select(transaction => new StatementTransaction
        {
            Type = transaction.Type,
            Amount = transaction.Amount,
            Timestamp = transaction.Timestamp
        });
        var openingBalance = account.Balance - allTransactions
            .Where(transaction => !from.HasValue || transaction.Timestamp >= from.Value)
            .Sum(signedAmount);

        return new AccountStatement
        {
            AccountNumber = account.AccountNumber,
            HolderName = account.HolderName,
            Currency = account.Currency,
            CurrentBalance = account.Balance,
            OpeningBalance = openingBalance,
            ClosingBalance = openingBalance + filteredTransactions.Sum(signedAmount),
            CreatedAt = account.CreatedAt,
            Transactions = filteredTransactions
        };
    }

    public async Task<AdminSummary> GetAdminSummaryAsync()
    {
        var accounts = await _dbContext.BankAccounts.AsNoTracking().ToListAsync();
        var totalBalance = accounts.Sum(account => account.Balance);

        return new AdminSummary
        {
            TotalAccounts = accounts.Count,
            ActiveAccounts = accounts.Count(account => account.IsActive),
            TotalBalance = totalBalance,
            CurrencyBreakdown = accounts
                .GroupBy(account => account.Currency)
                .Select(group => new CurrencySummary
                {
                    Currency = group.Key,
                    Balance = group.Sum(account => account.Balance),
                    AccountCount = group.Count()
                })
                .OrderBy(item => item.Currency)
                .ToList()
        };
    }

    public async Task<BankAccount> CreateAccountAsync(string userId, string holderName, decimal initialDeposit, string currency)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            throw new InvalidOperationException("Holder name is required.");
        }

        if (initialDeposit < 0m)
        {
            throw new InvalidOperationException("Initial deposit cannot be negative.");
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        var account = new BankAccount
        {
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            HolderName = holderName.Trim(),
            Currency = normalizedCurrency,
            Balance = 0m,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        if (initialDeposit > 0m)
        {
            account.Balance = initialDeposit;
            account.Transactions.Add(new BankTransaction
            {
                Type = "deposit",
                Amount = initialDeposit,
                Description = "Initial deposit",
                Timestamp = DateTime.UtcNow,
                RelatedAccountNumber = account.AccountNumber
            });
        }

        _dbContext.BankAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        return await _dbContext.BankAccounts
            .Include(item => item.Transactions.OrderBy(transaction => transaction.Timestamp))
            .SingleAsync(item => item.Id == account.Id);
    }

    public async Task<BankAccount> DepositAsync(string userId, string accountNumber, decimal amount, string description)
    {
        ValidatePositiveAmount(amount);

        var account = await _dbContext.BankAccounts
            .Include(item => item.Transactions)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.AccountNumber == accountNumber);

        if (account is null)
        {
            throw new KeyNotFoundException();
        }

        account.Balance += amount;
        account.Transactions.Add(new BankTransaction
        {
            Type = "deposit",
            Amount = amount,
            Description = description,
            Timestamp = DateTime.UtcNow,
            RelatedAccountNumber = account.AccountNumber
        });

        await _dbContext.SaveChangesAsync();
        return account;
    }

    public async Task<BankAccount> WithdrawAsync(string userId, string accountNumber, decimal amount, string description)
    {
        ValidatePositiveAmount(amount);

        var account = await _dbContext.BankAccounts
            .Include(item => item.Transactions)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.AccountNumber == accountNumber);

        if (account is null)
        {
            throw new KeyNotFoundException();
        }

        if (account.Balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds for this withdrawal.");
        }

        account.Balance -= amount;
        account.Transactions.Add(new BankTransaction
        {
            Type = "withdrawal",
            Amount = amount,
            Description = description,
            Timestamp = DateTime.UtcNow,
            RelatedAccountNumber = account.AccountNumber
        });

        await _dbContext.SaveChangesAsync();
        return account;
    }

    public async Task<TransferResult> TransferAsync(string userId, string fromAccountNumber, string toAccountNumber, decimal amount, string description)
    {
        if (string.IsNullOrWhiteSpace(fromAccountNumber) || string.IsNullOrWhiteSpace(toAccountNumber))
        {
            throw new InvalidOperationException("Both source and destination account numbers are required.");
        }

        if (string.Equals(fromAccountNumber, toAccountNumber, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and destination accounts must be different.");
        }

        ValidatePositiveAmount(amount);

        var fromAccount = await _dbContext.BankAccounts
            .Include(account => account.Transactions)
            .FirstOrDefaultAsync(account => account.UserId == userId && account.AccountNumber == fromAccountNumber);

        if (fromAccount is null)
        {
            throw new KeyNotFoundException();
        }

        var toAccount = await _dbContext.BankAccounts
            .Include(account => account.Transactions)
            .FirstOrDefaultAsync(account => account.UserId == userId && account.IsActive && account.AccountNumber == toAccountNumber);

        if (toAccount is null)
        {
            throw new KeyNotFoundException();
        }

        if (fromAccount.Balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds for this transfer.");
        }

        if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Transfers require both accounts to use the same currency.");
        }

        var now = DateTime.UtcNow;
        var transferDescription = string.IsNullOrWhiteSpace(description) ? "Transfer" : description;

        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        fromAccount.Transactions.Add(new BankTransaction
        {
            Type = "transfer_out",
            Amount = amount,
            Description = $"{transferDescription} to {toAccount.AccountNumber}",
            Timestamp = now,
            RelatedAccountNumber = toAccount.AccountNumber
        });

        toAccount.Transactions.Add(new BankTransaction
        {
            Type = "transfer_in",
            Amount = amount,
            Description = $"{transferDescription} from {fromAccount.AccountNumber}",
            Timestamp = now,
            RelatedAccountNumber = fromAccount.AccountNumber
        });

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return new TransferResult
        {
            FromAccountNumber = fromAccount.AccountNumber,
            ToAccountNumber = toAccount.AccountNumber,
            Amount = amount,
            Description = transferDescription,
            Timestamp = now
        };
    }

    private static void ValidatePositiveAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }
    }

    private static string GenerateAccountNumber()
    {
        return $"SB-{Random.Shared.NextInt64(10000000, 99999999):D8}";
    }
}

public sealed class BankDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasIndex(account => account.AccountNumber).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_BankAccounts_Balance_NonNegative", "\"Balance\" >= 0");
                table.HasCheckConstraint("CK_BankAccounts_Currency_Length", "length(\"Currency\") = 3");
            });
            entity.HasOne(account => account.User)
                .WithMany()
                .HasForeignKey(account => account.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankAccount>()
            .HasMany(account => account.Transactions)
            .WithOne(transaction => transaction.Account)
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_BankTransactions_Amount_Positive", "\"Amount\" > 0");
            });
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        });
    }
}

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}

public sealed class BankAccount
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ApplicationUser? User { get; set; }

    public List<BankTransaction> Transactions { get; set; } = new();
}

public sealed class BankTransaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RelatedAccountNumber { get; set; }

    [JsonIgnore]
    public BankAccount? Account { get; set; }
}

public sealed class AdminSummary
{
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public decimal TotalBalance { get; set; }
    public List<CurrencySummary> CurrencyBreakdown { get; set; } = new();
}

public sealed class CurrencySummary
{
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int AccountCount { get; set; }
}

public sealed class AccountStatement
{
    public string AccountNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal CurrentBalance { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StatementTransaction> Transactions { get; set; } = new();
}

public sealed class StatementTransaction
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? RelatedAccountNumber { get; set; }
}

public sealed class RegisterRequest
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateAccountRequest
{
    [JsonPropertyName("holderName")]
    public string HolderName { get; set; } = string.Empty;

    [JsonPropertyName("initialDeposit")]
    public decimal InitialDeposit { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public sealed class DepositRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class WithdrawRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class TransferRequest
{
    [JsonPropertyName("fromAccountNumber")]
    public string FromAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("toAccountNumber")]
    public string ToAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class TransferResult
{
    [JsonPropertyName("fromAccountNumber")]
    public string FromAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("toAccountNumber")]
    public string ToAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed record AccountSummaryDto(
    string AccountNumber,
    string HolderName,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTime CreatedAt);

public sealed class OpenBankingExchangeRequest
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public sealed class SpendingInsightsRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("consentToShareTransactions")]
    public bool ConsentToShareTransactions { get; set; }
}

public sealed class MarketExplanationRequest
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class AssistantChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
