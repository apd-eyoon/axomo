using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Models.BambooHr;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Services.Clients;

public sealed class BambooHrService(
    HttpClient httpClient,
    IOptionsMonitor<BambooHrOptions> options,
    ILogger<BambooHrService> logger) : IBambooHrService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BambooCompanyInfo?> GetCompanyInformationAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/v1/company_information");
        return await SendAsync<BambooCompanyInfo>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<BambooEmployee>> GetEmployeeDirectoryAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/v1/employees/directory");
        var response = await SendAsync<BambooDirectoryResponse>(request, cancellationToken);
        return response?.Employees ?? [];
    }

    public async Task<BambooEmployee?> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/employees/{Uri.EscapeDataString(employeeId)}");
        return await SendAsync<BambooEmployee>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<BambooDependent>> GetEmployeeDependentsAsync(string employeeId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/employees/{Uri.EscapeDataString(employeeId)}/dependents");
        var response = await SendAsync<List<BambooDependent>>(request, cancellationToken);
        return response ?? [];
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var currentOptions = options.CurrentValue;
        var request = new HttpRequestMessage(method, CreateEndpointUri(relativePath, currentOptions));
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{currentOptions.ApiKey}:x"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static Uri CreateEndpointUri(string relativePath, BambooHrOptions options)
    {
        var baseUrl = GetBaseUrl(options);
        var normalizedBase = baseUrl.TrimEnd('/') + "/";
        var normalizedPath = relativePath.TrimStart('/');
        return new Uri(new Uri(normalizedBase, UriKind.Absolute), normalizedPath);
    }

    private static string GetBaseUrl(BambooHrOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CompanyDomain))
        {
            var companyDomain = options.CompanyDomain.Trim();
            if (Uri.TryCreate(companyDomain, UriKind.Absolute, out _))
            {
                return companyDomain;
            }

            return $"https://{companyDomain.TrimEnd('/')}.bamboohr.com";
        }

        return options.BaseUrl;
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("BambooHR request {Method} {Path}", request.Method, request.RequestUri);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("BambooHR response {StatusCode} {Path}", (int)response.StatusCode, request.RequestUri);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
