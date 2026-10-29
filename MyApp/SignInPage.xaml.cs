using Microsoft.Extensions.DependencyInjection;
using MyApp.Services;

namespace MyApp;

public partial class SignInPage : ContentPage
{
    private readonly BankingApiClient api = App.ServiceProvider.GetRequiredService<BankingApiClient>();
    private bool isRegistering;
    private bool isSubmitting;

    public SignInPage() => InitializeComponent();

    private void OnModeClicked(object? sender, EventArgs e)
    {
        isRegistering = !isRegistering;
        FullNameEntry.IsVisible = isRegistering;
        SubmitButton.Text = isRegistering ? "Create demo account" : "Sign in";
        ModeButton.Text = isRegistering ? "I already have an account" : "Create a demo account";
        StatusLabel.IsVisible = false;
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (isSubmitting) return;
        isSubmitting = true;
        SubmitButton.IsEnabled = false;
        StatusLabel.IsVisible = false;
        try
        {
            if (isRegistering)
            {
                await api.RegisterAsync(FullNameEntry.Text ?? "", EmailEntry.Text ?? "", PasswordEntry.Text ?? "");
            }
            else
            {
                await api.SignInAsync(EmailEntry.Text ?? "", PasswordEntry.Text ?? "");
            }

            if (Application.Current?.Windows.Count > 0)
                Application.Current.Windows[0].Page = new AppShell();
        }
        catch (Exception exception)
        {
            StatusLabel.Text = exception is HttpRequestException
                ? $"{exception.Message} Check that the StudentBank server is running on port 5080."
                : exception.Message;
            StatusLabel.IsVisible = true;
        }
        finally
        {
            isSubmitting = false;
            SubmitButton.IsEnabled = true;
        }
    }
}
