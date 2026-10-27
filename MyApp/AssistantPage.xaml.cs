using System.Collections.ObjectModel;
using MyApp.Models;
using MyApp.Services;

namespace MyApp;

public partial class AssistantPage : ContentPage
{
    private readonly BankingAssistantService assistantService = new();
    private bool isSending;

    public ObservableCollection<AssistantChatMessage> Messages { get; } = new()
    {
        new("StudentBank assistant", "Hi! I can explain everyday banking and help you think through a student budget. I cannot access your account or make payments.", false)
    };

    public AssistantPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        await SendQuestionAsync(MessageEntry.Text);
    }

    private async void OnPromptClicked(object? sender, EventArgs e)
    {
        if (sender is Button button)
            await SendQuestionAsync(button.CommandParameter?.ToString());
    }

    private async Task SendQuestionAsync(string? question)
    {
        var text = question?.Trim();
        if (isSending || string.IsNullOrWhiteSpace(text))
            return;

        isSending = true;
        MessageEntry.Text = string.Empty;
        SendButton.IsEnabled = false;
        ReplyIndicator.IsVisible = true;
        ReplyIndicator.IsRunning = true;
        Messages.Add(new AssistantChatMessage("You", text, true));
        ScrollToLatest();

        try
        {
            var reply = await assistantService.AskAsync(text);
            Messages.Add(new AssistantChatMessage("StudentBank assistant", reply, false));
        }
        catch (InvalidOperationException exception)
        {
            Messages.Add(new AssistantChatMessage("Setup needed", exception.Message, false));
        }
        catch (HttpRequestException)
        {
            Messages.Add(new AssistantChatMessage("Connection problem", "I couldn't reach the StudentBank server. Start it on port 5080 and try again.", false));
        }
        catch (TaskCanceledException)
        {
            Messages.Add(new AssistantChatMessage("Connection problem", "The request took too long. Check the server connection and try again.", false));
        }
        finally
        {
            isSending = false;
            SendButton.IsEnabled = true;
            ReplyIndicator.IsRunning = false;
            ReplyIndicator.IsVisible = false;
            ScrollToLatest();
        }
    }

    private void ScrollToLatest()
    {
        if (Messages.Count > 0)
            MessagesView.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
    }
}
