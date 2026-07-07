namespace StoreCreditor.Data.Entities;

/// <summary>
/// Employee discovered from BambooHR and awaiting store-credit processing.
/// </summary>
public sealed class EmployeeStaging
{
    public int Id { get; set; }

    public required string EmployeeId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? PreferredName { get; set; }

    public required string Email { get; set; }

    public string? Department { get; set; }

    public string? Division { get; set; }

    public string? Location { get; set; }

    public string? JobTitle { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool Processed { get; set; }

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedDate { get; set; }
}
