using System.Text.Json.Serialization;

namespace MyApp;

public sealed class Pokemon
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("types")]
    public List<PokemonTypeSlot> Types { get; set; } = [];

    [JsonIgnore]
    public string DisplayName => $"#{Id} {Name.ToUpperInvariant()}";

    [JsonIgnore]
    public string TypesLabel => string.Join(", ", Types.Select(type => type.Type.Name));
}

public sealed class PokemonTypeSlot
{
    [JsonPropertyName("type")]
    public PokemonType Type { get; set; } = new();
}

public sealed class PokemonType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
