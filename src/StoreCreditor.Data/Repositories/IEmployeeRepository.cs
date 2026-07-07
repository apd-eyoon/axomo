using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public interface IEmployeeRepository
{
    Task<bool> ExistsAsync(string employeeId, CancellationToken cancellationToken);

    Task AddAsync(EmployeeStaging employee, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeStaging>> GetPendingAsync(CancellationToken cancellationToken);

    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    Task<int> CountProcessedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeStaging>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
