using System.Text.Json.Serialization;

namespace StoreCreditor.Services.Models.BambooHr;

public sealed class BambooEmployee
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("preferredName")]
    public string? PreferredName { get; set; }

    [JsonPropertyName("workEmail")]
    public string? WorkEmail { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("division")]
    public string? Division { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("hireDate")]
    public DateOnly? HireDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
