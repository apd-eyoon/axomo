namespace StoreCreditor.Services.Options;

public sealed class AxomoOptions
{
    public const string SectionName = "Axomo";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string BearerToken { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
