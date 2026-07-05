using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Organizations;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.LogoUrl).HasMaxLength(2000);
        builder.Property(o => o.SubscriptionPlan).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.ActiveModules);

        builder.Property(o => o.ChamberOfCommerceNumber)
            .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : ChamberOfCommerceNumber.Of(v))
            .HasColumnName("ChamberOfCommerceNumber")
            .HasMaxLength(8);
    }
}
