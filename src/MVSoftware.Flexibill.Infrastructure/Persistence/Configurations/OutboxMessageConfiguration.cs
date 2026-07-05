using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Infrastructure.Persistence.Outbox;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);

        // De Worker haalt onverwerkte berichten op in tijdsvolgorde (Technisch Ontwerp, hoofdstuk 12).
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc });
    }
}
