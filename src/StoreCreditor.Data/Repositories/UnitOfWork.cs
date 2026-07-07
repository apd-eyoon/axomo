namespace StoreCreditor.Data.Repositories;

public sealed class UnitOfWork(StoreCreditorDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
