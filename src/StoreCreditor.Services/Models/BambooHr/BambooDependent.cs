using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.BambooHr;

public sealed class BambooDependent
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }
}
