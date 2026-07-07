using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.BambooHr;

public sealed class BambooDirectoryResponse
{
    [JsonPropertyName("employees")]
    public List<BambooEmployee> Employees { get; set; } = [];
}
