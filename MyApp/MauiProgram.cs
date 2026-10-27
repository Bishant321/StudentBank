using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
