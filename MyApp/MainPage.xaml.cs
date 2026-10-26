namespace MyApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        Loaded += async (_, _) => await TaskStore.InitializeAsync();
    }

    public System.Collections.ObjectModel.ObservableCollection<TaskItem> Tasks => TaskStore.Tasks;

    private async void OnAddTaskClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddTaskPage));
    }

    private async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TaskItem task)
        {
            if (sender is CollectionView collectionView)
                collectionView.SelectedItem = null;

            await Shell.Current.GoToAsync($"{nameof(DetailPage)}?taskId={task.Id}");
        }
    }
}
