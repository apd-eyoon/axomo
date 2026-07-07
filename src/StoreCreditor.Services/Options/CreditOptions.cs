namespace StoreCreditor.Services.Options;

public sealed class CreditOptions
{
    public const string SectionName = "Credit";

    public decimal NewEmployeeAmount { get; set; } = 50m;

    public string NewEmployeeDescription { get; set; } = "New Employee Store Credit";
}
