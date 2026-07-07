namespace StoreCreditor.Services.Interfaces;

public interface ICreditIssuingService
{
    Task<int> IssuePendingCreditsAsync(CancellationToken cancellationToken);
}
