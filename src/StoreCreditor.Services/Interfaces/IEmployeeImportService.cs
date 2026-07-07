namespace StoreCreditor.Services.Interfaces;

public interface IEmployeeImportService
{
    Task<int> ImportNewEmployeesAsync(CancellationToken cancellationToken);
}
