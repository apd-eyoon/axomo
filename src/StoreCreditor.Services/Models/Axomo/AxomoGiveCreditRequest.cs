using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.Axomo;

public sealed class AxomoGiveCreditRequest
{
    [JsonPropertyName("Email")]
    public required string Email { get; set; }

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("First")]
    public required string First { get; set; }

    [JsonPropertyName("Last")]
    public required string Last { get; set; }

    [JsonPropertyName("SendEmail")]
    public bool SendEmail { get; set; } = true;

    [JsonPropertyName("Description")]
    public required string Description { get; set; }
}
