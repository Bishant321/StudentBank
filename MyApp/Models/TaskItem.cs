using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyApp;

public sealed class TaskItem : INotifyPropertyChanged
{
    private bool isCompleted;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime DueDate { get; init; } = DateTime.Today;
    public string DueLabel => $"Due {DueDate:ddd, d MMM}";
    public string StatusLabel => IsCompleted ? "Completed" : "To do";

    public bool IsCompleted
    {
        get => isCompleted;
        set
        {
            if (isCompleted == value)
                return;
            isCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabel));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class TaskStore
{
    public static ObservableCollection<TaskItem> Tasks { get; } = new()
    {
        new TaskItem
        {
            Title = "Review the MAUI sample",
            Description = "Look through the app shell and page navigation.",
            DueDate = DateTime.Today.AddDays(1)
        },
        new TaskItem
        {
            Title = "Plan the app pages",
            Description = "Decide what each page shows and where its buttons navigate.",
            DueDate = DateTime.Today.AddDays(2)
        }
    };
}
