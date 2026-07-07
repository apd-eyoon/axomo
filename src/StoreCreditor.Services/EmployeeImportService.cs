using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Services;

public sealed class EmployeeImportService(
    IBambooHrService bambooHrService,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IOptionsMonitor<FeatureFlagOptions> featureFlags) : IEmployeeImportService
{
    public async Task<int> ImportNewEmployeesAsync(CancellationToken cancellationToken)
    {
        var currentFeatureFlags = featureFlags.CurrentValue;
        if (!currentFeatureFlags.EnableEmployeeImport || currentFeatureFlags.PauseJobs)
        {
            await auditService.LogAsync("Information", "EmployeeImport", "Skipped", "Employee import is disabled by feature flag.", cancellationToken: cancellationToken);
            return 0;
        }

        var employees = await bambooHrService.GetEmployeeDirectoryAsync(cancellationToken);
        var stagingEmails = currentFeatureFlags.GetStagingEmployeeEmailSet();
        if (currentFeatureFlags.StagingMode)
        {
            if (stagingEmails.Count == 0)
            {
                await auditService.LogAsync(
                    "Warning",
                    "EmployeeImport",
                    "Skipped",
                    "Staging mode is enabled, but no staging employee emails are configured.",
                    cancellationToken: cancellationToken);
                return 0;
            }

            employees = employees
                .Where(employee => IsStagingEmployee(employee.WorkEmail, stagingEmails))
                .ToList();
        }

        var imported = 0;

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.Id) || string.IsNullOrWhiteSpace(employee.WorkEmail))
            {
                continue;
            }

            if (await employeeRepository.ExistsAsync(employee.Id, cancellationToken))
            {
                continue;
            }

            await employeeRepository.AddAsync(new EmployeeStaging
            {
                EmployeeId = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                PreferredName = employee.PreferredName,
                Email = employee.WorkEmail,
                Department = employee.Department,
                Division = employee.Division,
                Location = employee.Location,
                JobTitle = employee.JobTitle,
                HireDate = employee.HireDate,
                IsActive = string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(employee.Status),
                Processed = false
            }, cancellationToken);
            imported++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync(
            "Information",
            "EmployeeImport",
            "Completed",
            $"Imported {imported} new employees.",
            new
            {
                Imported = imported,
                Scanned = employees.Count,
                currentFeatureFlags.StagingMode,
                StagingEmailCount = stagingEmails.Count
            },
            cancellationToken: cancellationToken);
        return imported;
    }

    private static bool IsStagingEmployee(string? email, HashSet<string> stagingEmails) =>
        !string.IsNullOrWhiteSpace(email) && stagingEmails.Contains(email.Trim());
}
