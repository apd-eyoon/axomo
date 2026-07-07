using StoreCreditor.Services.Models.BambooHr;

namespace StoreCreditor.Services.Interfaces;

public interface IBambooHrService
{
    Task<BambooCompanyInfo?> GetCompanyInformationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<BambooEmployee>> GetEmployeeDirectoryAsync(CancellationToken cancellationToken);

    Task<BambooEmployee?> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BambooDependent>> GetEmployeeDependentsAsync(string employeeId, CancellationToken cancellationToken);
}
