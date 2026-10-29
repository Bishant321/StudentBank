using Microsoft.Extensions.DependencyInjection;

namespace MyApp;

public partial class App : Application
{
	public static IServiceProvider ServiceProvider { get; set; } = default!;

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new SignInPage()));
	}
}
