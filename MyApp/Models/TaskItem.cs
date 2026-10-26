using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Maui.Storage;

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
    private static readonly SemaphoreSlim persistenceLock = new(1, 1);
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private static bool initialized;

    public static ObservableCollection<TaskItem> Tasks { get; } = new();

    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, "tasks.json");

    public static async Task InitializeAsync()
    {
        await persistenceLock.WaitAsync();
        try
        {
            if (initialized)
                return;

            if (File.Exists(StoragePath))
            {
                await using var input = File.OpenRead(StoragePath);
                var savedTasks = await JsonSerializer.DeserializeAsync<List<TaskItem>>(input, jsonOptions);
                if (savedTasks is not null)
                {
                    foreach (var task in savedTasks)
                        Tasks.Add(task);
                }
            }
            else
            {
                AddSampleTasks();
                await WriteTasksAsync();
            }

            initialized = true;
        }
        finally
        {
            persistenceLock.Release();
        }
    }

    public static async Task SaveAsync()
    {
        await persistenceLock.WaitAsync();
        try
        {
            await WriteTasksAsync();
        }
        finally
        {
            persistenceLock.Release();
        }
    }

    private static async Task WriteTasksAsync()
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        var temporaryPath = StoragePath + ".tmp";
        await using (var output = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(output, Tasks.ToList(), jsonOptions);
        File.Move(temporaryPath, StoragePath, overwrite: true);
    }

    private static void AddSampleTasks()
    {
        Tasks.Add(new TaskItem
        {
            Title = "Review the MAUI sample",
            Description = "Look through the app shell and page navigation.",
            DueDate = DateTime.Today.AddDays(1)
        });
        Tasks.Add(new TaskItem
        {
            Title = "Plan the app pages",
            Description = "Decide what each page shows and where its buttons navigate.",
            DueDate = DateTime.Today.AddDays(2)
        });
    }
}
