using System.Net;
using System.Net.Http.Json;

namespace MyApp;

public sealed class PokemonService(HttpClient httpClient)
{
    public async Task<Pokemon> GetPokemonAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var pokemon = Uri.EscapeDataString(nameOrId.Trim().ToLowerInvariant());

        try
        {
            return await httpClient.GetFromJsonAsync<Pokemon>($"pokemon/{pokemon}", cancellationToken)
                ?? throw new InvalidDataException("The Pokémon service returned an empty response.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("No Pokémon matched that name or number.", exception);
        }
    }
}
