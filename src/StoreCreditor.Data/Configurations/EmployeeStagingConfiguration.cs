using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Configurations;

public sealed class EmployeeStagingConfiguration : IEntityTypeConfiguration<EmployeeStaging>
{
    public void Configure(EntityTypeBuilder<EmployeeStaging> builder)
    {
        builder.ToTable("EmployeeStaging");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PreferredName).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(150);
        builder.Property(x => x.Division).HasMaxLength(150);
        builder.Property(x => x.Location).HasMaxLength(150);
        builder.Property(x => x.JobTitle).HasMaxLength(150);
        builder.HasIndex(x => x.EmployeeId).IsUnique();
        builder.HasIndex(x => new { x.Processed, x.IsActive });
    }
}
