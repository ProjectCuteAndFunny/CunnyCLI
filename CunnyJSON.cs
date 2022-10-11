using System.Text.Json.Serialization;

namespace GermanBread.CunnyCLI;

public sealed class CunnyJsonElement {
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("post_url")]
    public string PostUrl { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }
}