using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Services;

namespace MyApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddHttpClient<PokemonService>(client =>
		{
			client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
		});
		builder.Services.AddTransient<PokemonPage>();
		builder.Services.AddSingleton<BankingApiClient>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		MyApp.App.ServiceProvider = app.Services;
		return app;
	}
}
