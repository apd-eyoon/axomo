namespace StoreCreditor.Services.Options;

public sealed class BambooHrOptions
{
    public const string SectionName = "BambooHR";

    public string BaseUrl { get; set; } = string.Empty;

    public string CompanyDomain { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
