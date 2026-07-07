using Microsoft.EntityFrameworkCore;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public sealed class AuditLogRepository(StoreCreditorDbContext dbContext) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
        await dbContext.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
}
