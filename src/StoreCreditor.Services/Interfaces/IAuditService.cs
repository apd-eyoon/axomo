namespace StoreCreditor.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string level, string category, string action, string message, object? payload = null, Exception? exception = null, CancellationToken cancellationToken = default);
}
