namespace StoreCreditor.Data.Entities;

/// <summary>
/// Structured operational event persisted for compliance and troubleshooting.
/// </summary>
public sealed class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public required string Level { get; set; }

    public required string Category { get; set; }

    public required string Action { get; set; }

    public required string Message { get; set; }

    public string? Exception { get; set; }

    public string? Payload { get; set; }

    public string? CorrelationId { get; set; }
}
