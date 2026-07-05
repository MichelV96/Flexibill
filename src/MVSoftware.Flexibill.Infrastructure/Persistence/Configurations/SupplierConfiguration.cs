using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Suppliers;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrganizationId).IsRequired();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Category).HasMaxLength(100);

        builder.Property(s => s.ChamberOfCommerceNumber)
            .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : ChamberOfCommerceNumber.Of(v))
            .HasMaxLength(8);

        builder.Property(s => s.VatNumber)
            .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : VatNumber.Of(v))
            .HasMaxLength(20);

        builder.OwnsOne(s => s.PrimaryContact, contact =>
        {
            contact.Property(c => c.Name).HasColumnName("PrimaryContactName").HasMaxLength(200);
            contact.Property(c => c.Phone).HasColumnName("PrimaryContactPhone").HasMaxLength(50);
            contact.Property(c => c.Email)
                .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : EmailAddress.Of(v))
                .HasColumnName("PrimaryContactEmail")
                .HasMaxLength(320);
        });

        builder.OwnsOne(s => s.Address, address =>
        {
            address.Property(a => a.Street).HasMaxLength(200);
            address.Property(a => a.HouseNumber).HasMaxLength(20);
            address.Property(a => a.PostalCode).HasMaxLength(20);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.Country).HasMaxLength(2);
        });

        // Iban is een value object zonder eigen identiteit - eigen tabel met een samengestelde
        // sleutel (SupplierId + de IBAN-waarde zelf), aansluitend op Supplier.AddIban's
        // waarde-gebaseerde dedup (_ibans.Contains(iban)).
        builder.OwnsMany(s => s.Ibans, iban =>
        {
            iban.ToTable("SupplierIbans");
            iban.WithOwner().HasForeignKey("SupplierId");
            iban.Property(i => i.Value).HasColumnName("Iban").HasMaxLength(34);
            iban.HasKey("SupplierId", "Value");
        });

        builder.OwnsMany(s => s.BranchLinks, link =>
        {
            link.ToTable("SupplierBranchLinks");
            link.WithOwner().HasForeignKey("SupplierId");
            link.HasKey(l => l.Id);
            link.Property(l => l.BranchId).IsRequired();
        });
    }
}
