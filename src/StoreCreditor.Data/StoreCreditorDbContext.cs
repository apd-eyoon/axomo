using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data;

/// <summary>
/// Entity Framework Core database context for Identity, auditing, staging, and credit history.
/// </summary>
public sealed class StoreCreditorDbContext(DbContextOptions<StoreCreditorDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public DbSet<EmployeeStaging> EmployeeStaging => Set<EmployeeStaging>();

    public DbSet<EmployeeCreditHistory> EmployeeCreditHistory => Set<EmployeeCreditHistory>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(StoreCreditorDbContext).Assembly);
    }
}
