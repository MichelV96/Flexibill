using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Domain.Branches;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.OrganizationId).IsRequired();
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.AccountingPackage).HasConversion<string>().HasMaxLength(30);

        builder.OwnsOne(b => b.Address, address =>
        {
            address.Property(a => a.Street).HasMaxLength(200);
            address.Property(a => a.HouseNumber).HasMaxLength(20);
            address.Property(a => a.PostalCode).HasMaxLength(20);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.Country).HasMaxLength(2);
        });
    }
}
