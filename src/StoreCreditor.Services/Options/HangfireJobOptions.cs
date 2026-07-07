namespace StoreCreditor.Services.Options;

public sealed class HangfireJobOptions
{
    public const string SectionName = "Hangfire";

    public string EmployeeImportCron { get; set; } = "0 2 * * *";

    public string StoreCreditCron { get; set; } = "*/15 * * * *";
}
