namespace StoreCreditor.Services.Validation;

public sealed record EmployeeValidationResult(bool IsValid, string? FailureReason)
{
    public static EmployeeValidationResult Success { get; } = new(true, null);

    public static EmployeeValidationResult Failure(string reason) => new(false, reason);
}
