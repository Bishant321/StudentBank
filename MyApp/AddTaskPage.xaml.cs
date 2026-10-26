namespace MyApp;

public partial class AddTaskPage : ContentPage
{
    public AddTaskPage()
    {
        InitializeComponent();
        DueDatePicker.Date = DateTime.Today;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorLabel.IsVisible = true;
            return;
        }

        TaskStore.Tasks.Add(new TaskItem
        {
            Title = title,
            Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
            DueDate = DueDatePicker.Date ?? DateTime.Today
        });
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
