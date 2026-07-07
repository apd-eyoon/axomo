using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Repositories;

public interface IStoreCreditRepository
{
    Task<bool> HasSuccessfulNewEmployeeCreditAsync(string employeeId, string description, CancellationToken cancellationToken);

    Task<bool> HasNewEmployeeCreditAttemptAsync(string email, string description, CancellationToken cancellationToken);

    Task AddAsync(EmployeeCreditHistory history, CancellationToken cancellationToken);

    Task<int> CountSuccessfulAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeCreditHistory>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
