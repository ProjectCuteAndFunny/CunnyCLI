using System.Text.Json.Serialization;

namespace GermanBread.cunnycli;

public sealed class CunnyJSONElement {
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = "";
    [JsonPropertyName("image_url")]
    public string ImageURL { get; set; } = "";
    [JsonPropertyName("post_url")]
    public string PostURL { get; set; } = "";
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";
    [JsonPropertyName("height")]
    public int Height { get; set; } = 0;
    [JsonPropertyName("width")]
    public int Width { get; set; } = 0;
    [JsonPropertyName("id")]
    public int ID { get; set; } = 0;
}