using StoreCreditor.Services.Models.Axomo;

namespace StoreCreditor.Services.Interfaces;

public interface IAxomoService
{
    Task<AxomoUser?> GetUserAsync(string email, CancellationToken cancellationToken);

    Task<AxomoStoreCreditBalance?> GetStoreCreditBalanceAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<AxomoCreditLog>> GetCreditLogsAsync(string email, CancellationToken cancellationToken);

    Task<AxomoGiveCreditResponse> GiveCreditAsync(AxomoGiveCreditRequest request, CancellationToken cancellationToken);
}
