using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.Axomo;

public sealed class AxomoStoreCreditBalance
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}
