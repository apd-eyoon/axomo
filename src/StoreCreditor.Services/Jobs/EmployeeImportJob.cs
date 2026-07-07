using Microsoft.Extensions.Logging;
using StoreCreditor.Services.Interfaces;

namespace StoreCreditor.Services.Jobs;

public sealed class EmployeeImportJob(IEmployeeImportService importService, ILogger<EmployeeImportJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Employee import job started.");
        var imported = await importService.ImportNewEmployeesAsync(cancellationToken);
        logger.LogInformation("Employee import job completed. Imported {Imported} employees.", imported);
    }
}
