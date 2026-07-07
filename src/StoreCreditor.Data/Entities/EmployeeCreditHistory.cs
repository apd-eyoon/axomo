namespace StoreCreditor.Data.Entities;

/// <summary>
/// Immutable audit trail of every store-credit issuing attempt.
/// </summary>
public sealed class EmployeeCreditHistory
{
    public int Id { get; set; }

    public required string EmployeeId { get; set; }

    public required string Email { get; set; }

    public decimal Amount { get; set; }

    public required string Description { get; set; }

    public string? AxomoCreditId { get; set; }

    public DateTimeOffset IssuedDate { get; set; } = DateTimeOffset.UtcNow;

    public bool IssuedByJob { get; set; } = true;

    public bool Success { get; set; }

    public string? FailureReason { get; set; }
}
