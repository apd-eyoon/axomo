using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
