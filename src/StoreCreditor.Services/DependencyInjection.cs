using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using StoreCreditor.Services.Clients;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Jobs;
using StoreCreditor.Services.Options;
using StoreCreditor.Services.Validation;

namespace StoreCreditor.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreCreditorServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BambooHrOptions>(configuration.GetSection(BambooHrOptions.SectionName));
        services.Configure<AxomoOptions>(configuration.GetSection(AxomoOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<FeatureFlagOptions>(configuration.GetSection(FeatureFlagOptions.SectionName));
        services.Configure<CreditOptions>(configuration.GetSection(CreditOptions.SectionName));
        services.Configure<HangfireJobOptions>(configuration.GetSection(HangfireJobOptions.SectionName));

        services.AddScoped<IEmployeeImportService, EmployeeImportService>();
        services.AddScoped<ICreditIssuingService, CreditIssuingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmployeeValidator, EmployeeValidator>();
        services.AddScoped<EmployeeImportJob>();
        services.AddScoped<StoreCreditJob>();

        services.AddHttpClient<IBambooHrService, BambooHrService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<BambooHrOptions>>().CurrentValue;
            client.BaseAddress = CreateUri(GetBambooBaseUrl(options));
            client.Timeout = options.Timeout;
        }).AddPolicyHandler(CreateRetryPolicy());

        services.AddHttpClient<IAxomoService, AxomoService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<AxomoOptions>>().CurrentValue;
            client.BaseAddress = CreateUri(options.BaseUrl);
            client.Timeout = options.Timeout;
        }).AddPolicyHandler(CreateRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static Uri CreateUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : new Uri("https://localhost/");

    private static string GetBambooBaseUrl(BambooHrOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CompanyDomain))
        {
            return options.BaseUrl;
        }

        var companyDomain = options.CompanyDomain.Trim();
        return Uri.TryCreate(companyDomain, UriKind.Absolute, out _)
            ? companyDomain
            : $"https://{companyDomain.TrimEnd('/')}.bamboohr.com";
    }
}
