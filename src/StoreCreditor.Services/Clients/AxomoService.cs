using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Models.Axomo;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Services.Clients;

public sealed class AxomoService(
    HttpClient httpClient,
    IOptionsMonitor<AxomoOptions> options,
    ILogger<AxomoService> logger) : IAxomoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AxomoUser?> GetUserAsync(string email, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"User?email={Uri.EscapeDataString(email)}");
        return await SendAsync<AxomoUser>(request, cancellationToken);
    }

    public async Task<AxomoStoreCreditBalance?> GetStoreCreditBalanceAsync(string email, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"User/GetStoreCreditBalance?email={Uri.EscapeDataString(email)}");
        return await SendAsync<AxomoStoreCreditBalance>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<AxomoCreditLog>> GetCreditLogsAsync(string email, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"StoreCreditLog/GetLogsForUser?email={Uri.EscapeDataString(email)}");
        var response = await SendAsync<List<AxomoCreditLog>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<AxomoGiveCreditResponse> GiveCreditAsync(AxomoGiveCreditRequest requestModel, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "StoreCreditLog/GiveCredit");
        request.Content = JsonContent.Create(requestModel, options: JsonOptions);
        return await SendAsync<AxomoGiveCreditResponse>(request, cancellationToken)
            ?? new AxomoGiveCreditResponse { Success = false, Message = "Axomo returned an empty response." };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var currentOptions = options.CurrentValue;
        var request = new HttpRequestMessage(method, CreateEndpointUri(relativePath, currentOptions));
        if (!string.IsNullOrWhiteSpace(currentOptions.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentOptions.BearerToken);
        }

        if (!string.IsNullOrWhiteSpace(currentOptions.ApiKey))
        {
            request.Headers.Add("ApiKey", currentOptions.ApiKey);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static Uri CreateEndpointUri(string relativePath, AxomoOptions options)
    {
        var normalizedBase = options.BaseUrl.TrimEnd('/') + "/";
        var normalizedPath = relativePath.TrimStart('/');
        return new Uri(new Uri(normalizedBase, UriKind.Absolute), normalizedPath);
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Axomo request {Method} {Path}", request.Method, request.RequestUri);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Axomo response {StatusCode} {Path}", (int)response.StatusCode, request.RequestUri);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
