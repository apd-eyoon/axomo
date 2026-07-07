using Microsoft.EntityFrameworkCore;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public sealed class EmployeeRepository(StoreCreditorDbContext dbContext) : IEmployeeRepository
{
    public Task<bool> ExistsAsync(string employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeeStaging.AnyAsync(x => x.EmployeeId == employeeId, cancellationToken);

    public async Task AddAsync(EmployeeStaging employee, CancellationToken cancellationToken) =>
        await dbContext.EmployeeStaging.AddAsync(employee, cancellationToken);

    public async Task<IReadOnlyList<EmployeeStaging>> GetPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.EmployeeStaging
            .Where(x => !x.Processed)
            .OrderBy(x => x.CreatedDate)
            .ToListAsync(cancellationToken);

    public Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
        dbContext.EmployeeStaging.CountAsync(x => !x.Processed, cancellationToken);

    public Task<int> CountProcessedAsync(CancellationToken cancellationToken) =>
        dbContext.EmployeeStaging.CountAsync(x => x.Processed, cancellationToken);

    public async Task<IReadOnlyList<EmployeeStaging>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
        await dbContext.EmployeeStaging
            .OrderByDescending(x => x.CreatedDate)
            .Take(count)
            .ToListAsync(cancellationToken);
}
