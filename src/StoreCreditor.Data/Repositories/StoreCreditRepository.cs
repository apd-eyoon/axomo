using Microsoft.EntityFrameworkCore;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public sealed class StoreCreditRepository(StoreCreditorDbContext dbContext) : IStoreCreditRepository
{
    public Task<bool> HasSuccessfulNewEmployeeCreditAsync(string employeeId, string description, CancellationToken cancellationToken) =>
        dbContext.EmployeeCreditHistory.AnyAsync(
            x => x.EmployeeId == employeeId && x.Description == description && x.Success,
            cancellationToken);

    public Task<bool> HasNewEmployeeCreditAttemptAsync(string email, string description, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return dbContext.EmployeeCreditHistory.AnyAsync(
            x => x.Email.ToUpper() == normalizedEmail && x.Description == description,
            cancellationToken);
    }

    public async Task AddAsync(EmployeeCreditHistory history, CancellationToken cancellationToken) =>
        await dbContext.EmployeeCreditHistory.AddAsync(history, cancellationToken);

    public Task<int> CountSuccessfulAsync(CancellationToken cancellationToken) =>
        dbContext.EmployeeCreditHistory.CountAsync(x => x.Success, cancellationToken);

    public async Task<IReadOnlyList<EmployeeCreditHistory>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
        await dbContext.EmployeeCreditHistory
            .OrderByDescending(x => x.IssuedDate)
            .Take(count)
            .ToListAsync(cancellationToken);
}
