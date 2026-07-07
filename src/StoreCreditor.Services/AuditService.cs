using System.Text.Json;
using Microsoft.Extensions.Logging;
using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services.Interfaces;

namespace StoreCreditor.Services;

public sealed class AuditService(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(string level, string category, string action, string message, object? payload = null, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        logger.Log(MapLevel(level), exception, "{Category}.{Action}: {Message}", category, action, message);

        var auditLog = new AuditLog
        {
            Level = level,
            Category = category,
            Action = action,
            Message = message,
            Exception = exception?.ToString(),
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CorrelationId = Environment.CurrentManagedThreadId.ToString()
        };

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static LogLevel MapLevel(string level) =>
        Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed) ? parsed : LogLevel.Information;
}
