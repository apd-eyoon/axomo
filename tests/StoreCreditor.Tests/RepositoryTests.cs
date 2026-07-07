using Microsoft.EntityFrameworkCore;
using StoreCreditor.Data;
using StoreCreditor.Data.Entities;
using StoreCreditor.Data.Repositories;

namespace StoreCreditor.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task EmployeeRepository_CountsPendingAndProcessedEmployees()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EmployeeRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(NewEmployee("1", processed: false), CancellationToken.None);
        await repository.AddAsync(NewEmployee("2", processed: true), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, await repository.CountPendingAsync(CancellationToken.None));
        Assert.Equal(1, await repository.CountProcessedAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StoreCreditRepository_FindsSuccessfulNewEmployeeCredit()
    {
        await using var dbContext = CreateDbContext();
        var repository = new StoreCreditRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(new EmployeeCreditHistory
        {
            EmployeeId = "1",
            Email = "ada.lovelace@aimpointdigital.com",
            Amount = 50,
            Description = "New Employee Store Credit",
            Success = true
        }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var exists = await repository.HasSuccessfulNewEmployeeCreditAsync("1", "New Employee Store Credit", CancellationToken.None);

        Assert.True(exists);
    }

    private static StoreCreditorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreCreditorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StoreCreditorDbContext(options);
    }

    private static EmployeeStaging NewEmployee(string employeeId, bool processed) =>
        new()
        {
            EmployeeId = employeeId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = $"ada{employeeId}@aimpointdigital.com",
            Processed = processed
        };
}
