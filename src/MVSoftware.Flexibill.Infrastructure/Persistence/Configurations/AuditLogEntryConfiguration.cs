using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ChangedByDisplayName).HasMaxLength(200);
        builder.Property(a => a.Source).HasConversion<string>().HasMaxLength(10);

        // Append-only (hoofdstuk 15): geen update-/delete-pad in de applicatie zelf, maar dat
        // is een procesregel, geen databaseconstraint - hier bewust niet extra afgedwongen.
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
