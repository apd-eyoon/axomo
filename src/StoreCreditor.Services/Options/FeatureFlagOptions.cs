namespace StoreCreditor.Services.Options;

public sealed class FeatureFlagOptions
{
    public const string SectionName = "FeatureFlags";

    public bool PauseJobs { get; set; }

    public bool EnableCreditIssuing { get; set; } = true;

    public bool EnableEmployeeImport { get; set; } = true;

    public bool DryRunMode { get; set; }

    public bool StagingMode { get; set; }

    public string[] StagingEmployeeEmails { get; set; } = [];

    public HashSet<string> GetStagingEmployeeEmailSet() =>
        StagingEmployeeEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
