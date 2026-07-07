using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.BambooHr;

public sealed class BambooCompanyInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; set; }
}
