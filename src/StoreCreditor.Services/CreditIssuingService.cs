using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Models.Axomo;
using StoreCreditor.Services.Options;
using StoreCreditor.Services.Validation;

namespace StoreCreditor.Services;

public sealed class CreditIssuingService(
    IEmployeeRepository employeeRepository,
    IStoreCreditRepository storeCreditRepository,
    IUnitOfWork unitOfWork,
    IAxomoService axomoService,
    IAuditService auditService,
    IEmployeeValidator validator,
    IOptionsMonitor<FeatureFlagOptions> featureFlags,
    IOptionsMonitor<CreditOptions> creditOptions) : ICreditIssuingService
{
    public async Task<int> IssuePendingCreditsAsync(CancellationToken cancellationToken)
    {
        var currentFeatureFlags = featureFlags.CurrentValue;
        if (!currentFeatureFlags.EnableCreditIssuing || currentFeatureFlags.PauseJobs)
        {
            await auditService.LogAsync("Information", "StoreCredit", "Skipped", "Credit issuing is disabled by feature flag.", cancellationToken: cancellationToken);
            return 0;
        }

        var employees = await employeeRepository.GetPendingAsync(cancellationToken);
        var stagingEmails = currentFeatureFlags.GetStagingEmployeeEmailSet();
        if (currentFeatureFlags.StagingMode)
        {
            if (stagingEmails.Count == 0)
            {
                await auditService.LogAsync(
                    "Warning",
                    "StoreCredit",
                    "Skipped",
                    "Staging mode is enabled, but no staging employee emails are configured.",
                    cancellationToken: cancellationToken);
                return 0;
            }

            employees = employees
                .Where(employee => stagingEmails.Contains(employee.Email.Trim()))
                .ToList();
        }

        var issued = 0;

        foreach (var employee in employees)
        {
            var result = await ProcessEmployeeAsync(employee, currentFeatureFlags, cancellationToken);
            if (result)
            {
                issued++;
            }
        }

        return issued;
    }

    private async Task<bool> ProcessEmployeeAsync(
        EmployeeStaging employee,
        FeatureFlagOptions currentFeatureFlags,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(employee);
        if (!validation.IsValid)
        {
            await auditService.LogAsync("Warning", "StoreCredit", "ValidationFailed", validation.FailureReason ?? "Employee validation failed.", new { employee.EmployeeId, employee.Email }, cancellationToken: cancellationToken);
            return false;
        }

        var currentCreditOptions = creditOptions.CurrentValue;
        var description = currentCreditOptions.NewEmployeeDescription;
        if (await storeCreditRepository.HasSuccessfulNewEmployeeCreditAsync(employee.EmployeeId, description, cancellationToken))
        {
            MarkProcessed(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("Information", "StoreCredit", "DuplicateSkipped", "Local credit history already contains a successful new employee credit.", new { employee.EmployeeId }, cancellationToken: cancellationToken);
            return false;
        }

        if (await storeCreditRepository.HasNewEmployeeCreditAttemptAsync(employee.Email, description, cancellationToken))
        {
            MarkProcessed(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync(
                "Information",
                "StoreCredit",
                "DuplicateSkipped",
                "Local credit history already contains a new employee credit attempt for this email.",
                new { employee.EmployeeId, employee.Email },
                cancellationToken: cancellationToken);
            return false;
        }

        if (currentFeatureFlags.DryRunMode)
        {
            await auditService.LogAsync("Information", "StoreCredit", "DryRun", "Dry run mode skipped Axomo credit issuance.", new { employee.EmployeeId, employee.Email }, cancellationToken: cancellationToken);
            return false;
        }

        var request = new AxomoGiveCreditRequest
        {
            Email = employee.Email,
            Amount = currentCreditOptions.NewEmployeeAmount,
            First = GetAxomoFirstName(employee),
            Last = employee.LastName,
            SendEmail = true,
            Description = description
        };

        AxomoGiveCreditResponse response;
        try
        {
            response = await axomoService.GiveCreditAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await storeCreditRepository.AddAsync(new EmployeeCreditHistory
            {
                EmployeeId = employee.EmployeeId,
                Email = employee.Email,
                Amount = currentCreditOptions.NewEmployeeAmount,
                Description = description,
                IssuedDate = DateTimeOffset.UtcNow,
                IssuedByJob = true,
                Success = false,
                FailureReason = LimitFailureReason(exception.Message)
            }, cancellationToken);

            MarkProcessed(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("Error", "StoreCredit", "IssueFailed", "Axomo credit issuance threw an exception after the job made one attempt.", new { employee.EmployeeId, employee.Email }, exception, cancellationToken);
            return false;
        }

        await storeCreditRepository.AddAsync(new EmployeeCreditHistory
        {
            EmployeeId = employee.EmployeeId,
            Email = employee.Email,
            Amount = currentCreditOptions.NewEmployeeAmount,
            Description = description,
            AxomoCreditId = response.Id,
            IssuedDate = DateTimeOffset.UtcNow,
            IssuedByJob = true,
            Success = response.Success,
            FailureReason = response.Success ? null : LimitFailureReason(response.Message)
        }, cancellationToken);

        MarkProcessed(employee);

        if (response.Success)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("Information", "StoreCredit", "Issued", "Issued new employee store credit.", new { employee.EmployeeId, employee.Email, response.Id }, cancellationToken: cancellationToken);
            return true;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("Error", "StoreCredit", "IssueFailed", response.Message ?? "Axomo credit issuance failed.", new { employee.EmployeeId, employee.Email }, cancellationToken: cancellationToken);
        return false;
    }

    private static string GetAxomoFirstName(EmployeeStaging employee) =>
        string.IsNullOrWhiteSpace(employee.PreferredName) ? employee.FirstName : employee.PreferredName;

    private static void MarkProcessed(EmployeeStaging employee)
    {
        employee.Processed = true;
        employee.ProcessedDate = DateTimeOffset.UtcNow;
    }

    private static string? LimitFailureReason(string? message) =>
        string.IsNullOrEmpty(message) || message.Length <= 2000 ? message : message[..2000];
}
