using Microsoft.Extensions.DependencyInjection;
using StoreCreditor.Data.Repositories;

namespace StoreCreditor.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreCreditorData(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IStoreCreditRepository, StoreCreditRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
