using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using StoreCreditor.Services.Clients;
using StoreCreditor.Services.Models.Axomo;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Tests;

public sealed class HttpClientUriTests
{
    [Fact]
    public async Task BambooHrService_UsesCompanyDomainForFullDirectoryUri()
    {
        var handler = new CapturingHandler("""{"employees":[]}""");
        var service = new BambooHrService(
            new HttpClient(handler),
            new TestOptionsMonitor<BambooHrOptions>(new BambooHrOptions
            {
                BaseUrl = "https://api.bamboohr.com",
                CompanyDomain = "https://aimpointdigital.bamboohr.com",
                ApiKey = "api-key"
            }),
            NullLogger<BambooHrService>.Instance);

        await service.GetEmployeeDirectoryAsync(CancellationToken.None);

        Assert.Equal(
            "https://aimpointdigital.bamboohr.com/api/v1/employees/directory",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task BambooHrService_AcceptsBareCompanyDomain()
    {
        var handler = new CapturingHandler("""{"employees":[]}""");
        var service = new BambooHrService(
            new HttpClient(handler),
            new TestOptionsMonitor<BambooHrOptions>(new BambooHrOptions
            {
                BaseUrl = "https://api.bamboohr.com",
                CompanyDomain = "aimpointdigital",
                ApiKey = "api-key"
            }),
            NullLogger<BambooHrService>.Instance);

        await service.GetEmployeeDirectoryAsync(CancellationToken.None);

        Assert.Equal(
            "https://aimpointdigital.bamboohr.com/api/v1/employees/directory",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task AxomoService_PreservesBasePathForCreditLogsUriAndApiKeyHeader()
    {
        var handler = new CapturingHandler("[]");
        var service = new AxomoService(
            new HttpClient(handler),
            new TestOptionsMonitor<AxomoOptions>(new AxomoOptions
            {
                BaseUrl = "https://api.axomo.com/v3/aimpoint-digital",
                ApiKey = "api-key"
            }),
            NullLogger<AxomoService>.Instance);

        await service.GetCreditLogsAsync("test@aimpointdigital.com", CancellationToken.None);

        Assert.Equal(
            "https://api.axomo.com/v3/aimpoint-digital/StoreCreditLog/GetLogsForUser?email=test%40aimpointdigital.com",
            handler.LastRequest?.RequestUri?.ToString());
        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.TryGetValues("ApiKey", out var values));
        Assert.Equal("api-key", Assert.Single(values));
    }

    [Fact]
    public async Task AxomoService_SendsBearerTokenAndApiKeyWhenBothAreConfigured()
    {
        var handler = new CapturingHandler("[]");
        var service = new AxomoService(
            new HttpClient(handler),
            new TestOptionsMonitor<AxomoOptions>(new AxomoOptions
            {
                BaseUrl = "https://api.axomo.com/v3/aimpoint-digital",
                ApiKey = "api-key",
                BearerToken = "jwt-token"
            }),
            NullLogger<AxomoService>.Instance);

        await service.GetCreditLogsAsync("test@aimpointdigital.com", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.True(handler.LastRequest.Headers.TryGetValues("ApiKey", out var values));
        Assert.Equal("api-key", Assert.Single(values));
    }

    [Fact]
    public async Task AxomoService_UsesDocumentedStoreCreditBalanceUri()
    {
        var handler = new CapturingHandler("""{"email":"test@aimpointdigital.com","balance":1.23}""");
        var service = new AxomoService(
            new HttpClient(handler),
            new TestOptionsMonitor<AxomoOptions>(new AxomoOptions
            {
                BaseUrl = "https://api.axomo.com/v3/aimpoint-digital/",
                ApiKey = "api-key"
            }),
            NullLogger<AxomoService>.Instance);

        await service.GetStoreCreditBalanceAsync("test@aimpointdigital.com", CancellationToken.None);

        Assert.Equal(
            "https://api.axomo.com/v3/aimpoint-digital/User/GetStoreCreditBalance?email=test%40aimpointdigital.com",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task AxomoService_GiveCreditSendsRequiredPascalCasePayload()
    {
        var handler = new CapturingHandler("""{"id":"credit-123","success":true}""");
        var service = new AxomoService(
            new HttpClient(handler),
            new TestOptionsMonitor<AxomoOptions>(new AxomoOptions
            {
                BaseUrl = "https://api.axomo.com/v3/aimpoint-digital",
                ApiKey = "api-key"
            }),
            NullLogger<AxomoService>.Instance);

        await service.GiveCreditAsync(
            new AxomoGiveCreditRequest
            {
                Email = "test@aimpointdigital.com",
                Amount = 0.01m,
                First = "Ada",
                Last = "Lovelace",
                SendEmail = true,
                Description = "Testing"
            },
            CancellationToken.None);

        Assert.Equal(
            "https://api.axomo.com/v3/aimpoint-digital/StoreCreditLog/GiveCredit",
            handler.LastRequest?.RequestUri?.ToString());
        var json = handler.LastRequestBody;
        Assert.False(string.IsNullOrWhiteSpace(json));
        using var document = JsonDocument.Parse(json);
        Assert.Equal("test@aimpointdigital.com", document.RootElement.GetProperty("Email").GetString());
        Assert.Equal(0.01m, document.RootElement.GetProperty("Amount").GetDecimal());
        Assert.Equal("Ada", document.RootElement.GetProperty("First").GetString());
        Assert.Equal("Lovelace", document.RootElement.GetProperty("Last").GetString());
        Assert.True(document.RootElement.GetProperty("SendEmail").GetBoolean());
        Assert.Equal("Testing", document.RootElement.GetProperty("Description").GetString());
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
        }
    }
}
