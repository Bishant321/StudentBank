namespace MyApp;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class DetailPage : ContentPage
{
    private TaskItem? task;

    public DetailPage()
    {
        InitializeComponent();
    }

    public string TaskId
    {
        set
        {
            if (Guid.TryParse(Uri.UnescapeDataString(value), out var id))
            {
                task = TaskStore.Tasks.FirstOrDefault(item => item.Id == id);
                BindingContext = task;
                UpdateCompleteButton();
            }
        }
    }

    private void OnToggleCompletedClicked(object? sender, EventArgs e)
    {
        if (task is null)
            return;

        task.IsCompleted = !task.IsCompleted;
        UpdateCompleteButton();
    }

    private void UpdateCompleteButton() =>
        CompleteButton.Text = task?.IsCompleted == true ? "Mark to do" : "Mark complete";

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
