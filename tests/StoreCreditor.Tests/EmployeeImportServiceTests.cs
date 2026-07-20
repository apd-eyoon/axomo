using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Models.BambooHr;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Tests;

public sealed class EmployeeImportServiceTests
{
    [Fact]
    public async Task ImportNewEmployeesAsync_BaselinesExistingEmployeesAsInactiveAndProcessed()
    {
        var employees = new FakeEmployeeRepository();
        var service = CreateService(
            employees,
            new FeatureFlagOptions
            {
                EnableEmployeeImport = true,
                BaselineExistingEmployeesAsInactive = true
            },
            new BambooEmployee
            {
                Id = "123",
                FirstName = "Ada",
                LastName = "Lovelace",
                WorkEmail = "ada.lovelace@aimpointdigital.com"
            });

        var imported = await service.ImportNewEmployeesAsync(CancellationToken.None);

        Assert.Equal(1, imported);
        var employee = Assert.Single(employees.Added);
        Assert.False(employee.IsActive);
        Assert.True(employee.Processed);
        Assert.NotNull(employee.ProcessedDate);
        Assert.Null(employee.HireDate);
    }

    [Fact]
    public async Task ImportNewEmployeesAsync_ImportsMissingHireDateWhenEmployeeHasNotBeenStaged()
    {
        var employees = new FakeEmployeeRepository();
        var service = CreateService(
            employees,
            new FeatureFlagOptions
            {
                EnableEmployeeImport = true
            },
            new BambooEmployee
            {
                Id = "456",
                FirstName = "Grace",
                LastName = "Hopper",
                WorkEmail = "grace.hopper@aimpointdigital.com",
                Status = "Active"
            });

        var imported = await service.ImportNewEmployeesAsync(CancellationToken.None);

        Assert.Equal(1, imported);
        var employee = Assert.Single(employees.Added);
        Assert.True(employee.IsActive);
        Assert.False(employee.Processed);
        Assert.Null(employee.ProcessedDate);
        Assert.Null(employee.HireDate);
    }

    private static EmployeeImportService CreateService(
        FakeEmployeeRepository employees,
        FeatureFlagOptions featureFlags,
        params BambooEmployee[] bambooEmployees) =>
        new(
            new FakeBambooHrService(bambooEmployees),
            employees,
            new FakeUnitOfWork(),
            new FakeAuditService(),
            new TestOptionsMonitor<FeatureFlagOptions>(featureFlags));

    private sealed class FakeBambooHrService(IReadOnlyList<BambooEmployee> employees) : IBambooHrService
    {
        public Task<BambooCompanyInfo?> GetCompanyInformationAsync(CancellationToken cancellationToken) =>
            Task.FromResult<BambooCompanyInfo?>(null);

        public Task<IReadOnlyList<BambooEmployee>> GetEmployeeDirectoryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(employees);

        public Task<BambooEmployee?> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken) =>
            Task.FromResult<BambooEmployee?>(null);

        public Task<IReadOnlyList<BambooDependent>> GetEmployeeDependentsAsync(
            string employeeId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BambooDependent>>([]);
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        public List<EmployeeStaging> Added { get; } = [];

        public Task<bool> ExistsAsync(string employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(Added.Any(employee => employee.EmployeeId == employeeId));

        public Task AddAsync(EmployeeStaging employee, CancellationToken cancellationToken)
        {
            Added.Add(employee);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmployeeStaging>> GetPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeStaging>>(Added.Where(employee => !employee.Processed).ToList());

        public Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Added.Count(employee => !employee.Processed));

        public Task<int> CountProcessedAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Added.Count(employee => employee.Processed));

        public Task<IReadOnlyList<EmployeeStaging>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeStaging>>(Added.Take(count).ToList());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task LogAsync(
            string level,
            string category,
            string action,
            string message,
            object? payload = null,
            Exception? exception = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
