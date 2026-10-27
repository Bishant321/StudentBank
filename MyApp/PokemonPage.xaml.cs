using System.Collections.ObjectModel;

namespace MyApp;

public partial class PokemonPage : ContentPage
{
    private readonly PokemonService pokemonService;

    public PokemonPage(PokemonService pokemonService)
    {
        InitializeComponent();
        this.pokemonService = pokemonService;
        BindingContext = this;
    }

    public ObservableCollection<Pokemon> PokemonResults { get; } = [];

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        var query = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            ErrorLabel.Text = "Enter a Pokémon name or Pokédex number.";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        SearchEntry.IsEnabled = false;
        SearchButton.IsEnabled = false;
        SearchActivityIndicator.IsVisible = true;
        SearchActivityIndicator.IsRunning = true;

        try
        {
            var result = await pokemonService.GetPokemonAsync(query);
            var existingResult = PokemonResults.FirstOrDefault(pokemon => pokemon.Id == result.Id);
            if (existingResult is not null)
                PokemonResults.Remove(existingResult);
            PokemonResults.Insert(0, result);
        }
        catch (KeyNotFoundException exception)
        {
            ErrorLabel.Text = exception.Message;
            ErrorLabel.IsVisible = true;
        }
        catch (HttpRequestException)
        {
            ErrorLabel.Text = "Could not reach PokéAPI. Check your connection and try again.";
            ErrorLabel.IsVisible = true;
        }
        catch (Exception)
        {
            ErrorLabel.Text = "PokéAPI returned data the app could not read.";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            SearchEntry.IsEnabled = true;
            SearchButton.IsEnabled = true;
            SearchActivityIndicator.IsRunning = false;
            SearchActivityIndicator.IsVisible = false;
        }
    }
}
