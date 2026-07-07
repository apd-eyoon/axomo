using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Models.Axomo;
using StoreCreditor.Services.Options;
using StoreCreditor.Services.Validation;

namespace StoreCreditor.Tests;

public sealed class CreditIssuingServiceTests
{
    [Fact]
    public async Task IssuePendingCreditsAsync_CallsGiveCreditWithoutFetchingAxomoLogs()
    {
        var employees = new FakeEmployeeRepository(
            new EmployeeStaging
            {
                EmployeeId = "123",
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada.lovelace@aimpointdigital.com"
            });
        var storeCredits = new FakeStoreCreditRepository();
        var axomo = new FakeAxomoService();
        var service = new CreditIssuingService(
            employees,
            storeCredits,
            new FakeUnitOfWork(),
            axomo,
            new FakeAuditService(),
            new EmployeeValidator(),
            new TestOptionsMonitor<FeatureFlagOptions>(new FeatureFlagOptions
            {
                EnableCreditIssuing = true
            }),
            new TestOptionsMonitor<CreditOptions>(new CreditOptions
            {
                NewEmployeeAmount = 50,
                NewEmployeeDescription = "New Employee Store Credit"
            }));

        var issued = await service.IssuePendingCreditsAsync(CancellationToken.None);

        Assert.Equal(1, issued);
        Assert.Equal(1, axomo.GiveCreditCallCount);
        Assert.Equal(0, axomo.GetCreditLogsCallCount);
        Assert.Equal("Ada", axomo.LastGiveCreditRequest?.First);
        Assert.Equal("Lovelace", axomo.LastGiveCreditRequest?.Last);
        Assert.True(axomo.LastGiveCreditRequest?.SendEmail);
        Assert.Single(storeCredits.Added);
        Assert.True(employees.Pending[0].Processed);
    }

    [Fact]
    public async Task IssuePendingCreditsAsync_SkipsAxomoWhenEmailAlreadyHasCreditAttempt()
    {
        var employees = new FakeEmployeeRepository(
            new EmployeeStaging
            {
                EmployeeId = "456",
                FirstName = "Grace",
                LastName = "Hopper",
                Email = "grace.hopper@aimpointdigital.com"
            });
        var storeCredits = new FakeStoreCreditRepository
        {
            HasAttempt = true
        };
        var axomo = new FakeAxomoService();
        var service = CreateService(employees, storeCredits, axomo);

        var issued = await service.IssuePendingCreditsAsync(CancellationToken.None);

        Assert.Equal(0, issued);
        Assert.Equal(0, axomo.GiveCreditCallCount);
        Assert.Empty(storeCredits.Added);
        Assert.True(employees.Pending[0].Processed);
    }

    [Fact]
    public async Task IssuePendingCreditsAsync_RecordsAttemptAndStopsRetryingWhenAxomoFails()
    {
        var employees = new FakeEmployeeRepository(
            new EmployeeStaging
            {
                EmployeeId = "789",
                FirstName = "Katherine",
                LastName = "Johnson",
                Email = "katherine.johnson@aimpointdigital.com"
            });
        var storeCredits = new FakeStoreCreditRepository();
        var axomo = new FakeAxomoService
        {
            NextGiveCreditResponse = new AxomoGiveCreditResponse
            {
                Success = false,
                Message = "First Name is required."
            }
        };
        var service = CreateService(employees, storeCredits, axomo);

        var issued = await service.IssuePendingCreditsAsync(CancellationToken.None);

        Assert.Equal(0, issued);
        Assert.Equal(1, axomo.GiveCreditCallCount);
        var attempt = Assert.Single(storeCredits.Added);
        Assert.False(attempt.Success);
        Assert.Equal("First Name is required.", attempt.FailureReason);
        Assert.True(employees.Pending[0].Processed);
    }

    private static CreditIssuingService CreateService(
        FakeEmployeeRepository employees,
        FakeStoreCreditRepository storeCredits,
        FakeAxomoService axomo) =>
        new(
            employees,
            storeCredits,
            new FakeUnitOfWork(),
            axomo,
            new FakeAuditService(),
            new EmployeeValidator(),
            new TestOptionsMonitor<FeatureFlagOptions>(new FeatureFlagOptions
            {
                EnableCreditIssuing = true
            }),
            new TestOptionsMonitor<CreditOptions>(new CreditOptions
            {
                NewEmployeeAmount = 50,
                NewEmployeeDescription = "New Employee Store Credit"
            }));

    private sealed class FakeEmployeeRepository(params EmployeeStaging[] pending) : IEmployeeRepository
    {
        public IReadOnlyList<EmployeeStaging> Pending { get; } = pending;

        public Task<bool> ExistsAsync(string employeeId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddAsync(EmployeeStaging employee, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<EmployeeStaging>> GetPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Pending);

        public Task<int> CountPendingAsync(CancellationToken cancellationToken) => Task.FromResult(Pending.Count);

        public Task<int> CountProcessedAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<IReadOnlyList<EmployeeStaging>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeStaging>>(Pending.Take(count).ToList());
    }

    private sealed class FakeStoreCreditRepository : IStoreCreditRepository
    {
        public List<EmployeeCreditHistory> Added { get; } = [];

        public bool HasAttempt { get; init; }

        public bool HasSuccessfulCredit { get; init; }

        public Task<bool> HasSuccessfulNewEmployeeCreditAsync(
            string employeeId,
            string description,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasSuccessfulCredit);

        public Task<bool> HasNewEmployeeCreditAttemptAsync(
            string email,
            string description,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasAttempt);

        public Task AddAsync(EmployeeCreditHistory history, CancellationToken cancellationToken)
        {
            Added.Add(history);
            return Task.CompletedTask;
        }

        public Task<int> CountSuccessfulAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<IReadOnlyList<EmployeeCreditHistory>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeCreditHistory>>(Added.Take(count).ToList());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAxomoService : IAxomoService
    {
        public int GetCreditLogsCallCount { get; private set; }

        public int GiveCreditCallCount { get; private set; }

        public AxomoGiveCreditRequest? LastGiveCreditRequest { get; private set; }

        public AxomoGiveCreditResponse NextGiveCreditResponse { get; init; } = new()
        {
            Id = "credit-123",
            Success = true
        };

        public Task<AxomoUser?> GetUserAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<AxomoUser?>(null);

        public Task<AxomoStoreCreditBalance?> GetStoreCreditBalanceAsync(
            string email,
            CancellationToken cancellationToken) =>
            Task.FromResult<AxomoStoreCreditBalance?>(null);

        public Task<IReadOnlyList<AxomoCreditLog>> GetCreditLogsAsync(
            string email,
            CancellationToken cancellationToken)
        {
            GetCreditLogsCallCount++;
            return Task.FromResult<IReadOnlyList<AxomoCreditLog>>([]);
        }

        public Task<AxomoGiveCreditResponse> GiveCreditAsync(
            AxomoGiveCreditRequest request,
            CancellationToken cancellationToken)
        {
            GiveCreditCallCount++;
            LastGiveCreditRequest = request;
            return Task.FromResult(NextGiveCreditResponse);
        }
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
