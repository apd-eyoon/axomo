using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.Axomo;

public sealed class AxomoGiveCreditResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
