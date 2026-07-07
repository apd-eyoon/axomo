using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Level).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Exception).HasMaxLength(4000);
        builder.Property(x => x.Payload);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.Category, x.Action });
    }
}
