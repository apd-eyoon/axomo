using Microsoft.Extensions.Logging;
using StoreCreditor.Services.Interfaces;

namespace StoreCreditor.Services.Jobs;

public sealed class StoreCreditJob(ICreditIssuingService creditIssuingService, ILogger<StoreCreditJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Store credit job started.");
        var issued = await creditIssuingService.IssuePendingCreditsAsync(cancellationToken);
        logger.LogInformation("Store credit job completed. Issued {Issued} credits.", issued);
    }
}
