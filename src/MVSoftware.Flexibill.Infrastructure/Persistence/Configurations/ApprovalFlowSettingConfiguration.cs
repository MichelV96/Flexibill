using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Domain.Approvals;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class ApprovalFlowSettingConfiguration : IEntityTypeConfiguration<ApprovalFlowSetting>
{
    public void Configure(EntityTypeBuilder<ApprovalFlowSetting> builder)
    {
        builder.ToTable("ApprovalFlowSettings");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OrganizationId).IsRequired();
        builder.Property(f => f.BranchId).IsRequired();

        builder.OwnsMany(f => f.Levels, level =>
        {
            level.ToTable("ApprovalFlowLevels");
            level.WithOwner().HasForeignKey("ApprovalFlowSettingId");
            level.HasKey(l => l.Id);
            level.Property(l => l.Sequence).IsRequired();

            // MinimumAmount is optioneel (geldt dan altijd, FO 6.4) - EF Core ondersteunt
            // owned types die volledig null zijn als "geen waarde" (optional dependent).
            level.OwnsOne(l => l.MinimumAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("MinimumAmount").HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasColumnName("MinimumAmountCurrency").HasMaxLength(3);
            });
        });

        // ApprovalFlowSetting.Levels is een berekende IReadOnlyList (OrderBy + ToList over het
        // backing field _levels) - EF Core's backing-field-conventie matcht puur op naam
        // ("_levels" voor "Levels"), niet op de inhoud van de getter, dus dit wordt correct
        // op het echte veld gemapt (geverifieerd met een losse spike tegen SQLite in-memory).
    }
}
