using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Configurations;

public sealed class EmployeeCreditHistoryConfiguration : IEntityTypeConfiguration<EmployeeCreditHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeCreditHistory> builder)
    {
        builder.ToTable("EmployeeCreditHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AxomoCreditId).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.HasIndex(x => new { x.EmployeeId, x.Description, x.Success })
            .IsUnique()
            .HasFilter("[Success] = 1");
    }
}
