using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MyApp.Services;

public sealed class BankingAssistantService
{
    private readonly HttpClient httpClient;

    public BankingAssistantService()
    {
#if ANDROID
        httpClient = new HttpClient { BaseAddress = new Uri("http://10.0.2.2:5080/"), Timeout = TimeSpan.FromSeconds(45) };
#else
        httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5080/"), Timeout = TimeSpan.FromSeconds(45) };
#endif
    }

    public async Task<string> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/assistant/chat",
            new AssistantChatRequest(message),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode == 503)
                throw new InvalidOperationException("The AI service is not configured yet. Set OPENAI_API_KEY on the StudentBank server, then try again.");

            throw new HttpRequestException("The assistant could not answer right now. Please try again.", null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<AssistantChatResponse>(cancellationToken: cancellationToken);
        return result?.Reply ?? throw new InvalidOperationException("The assistant returned an empty reply.");
    }

    private sealed record AssistantChatRequest([property: JsonPropertyName("message")] string Message);
    private sealed record AssistantChatResponse([property: JsonPropertyName("reply")] string Reply);
}
