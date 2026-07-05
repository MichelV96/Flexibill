using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MVSoftware.Flexibill.Domain.Invoices;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrganizationId).IsRequired();
        builder.Property(i => i.BranchId).IsRequired();
        builder.Property(i => i.SupplierId).IsRequired();
        builder.Property(i => i.InvoiceNumber).HasMaxLength(100);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(i => i.ExternalBookingReference).HasMaxLength(200);

        // TotalAmountInclVat is afgeleid (TotalAmountExclVat + TotalVatAmount) en heeft geen
        // eigen backing field - niet persisteren.
        builder.Ignore(i => i.TotalAmountInclVat);

        // Twee Money-properties op dezelfde entiteit -> expliciete kolomnamen nodig, anders
        // botsen "Amount"/"Currency" van beide owned types.
        builder.OwnsOne(i => i.TotalAmountExclVat, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmountExclVat").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency).HasColumnName("TotalAmountExclVatCurrency").HasMaxLength(3);
        });
        builder.OwnsOne(i => i.TotalVatAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalVatAmount").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency).HasColumnName("TotalVatAmountCurrency").HasMaxLength(3);
        });

        builder.OwnsMany(i => i.Lines, line =>
        {
            line.ToTable("InvoiceLines");
            line.WithOwner().HasForeignKey("InvoiceId");
            line.HasKey(l => l.Id);

            line.Property(l => l.Description).IsRequired().HasMaxLength(500);
            line.Property(l => l.Quantity).HasColumnType("decimal(18,4)");
            line.Property(l => l.VatCode).HasMaxLength(20);
            line.Property(l => l.OcrConfidence).HasColumnType("decimal(5,4)");

            line.OwnsOne(l => l.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
            });
            line.OwnsOne(l => l.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
                money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3);
            });
        });

        builder.OwnsMany(i => i.ApprovalSteps, step =>
        {
            step.ToTable("InvoiceApprovalSteps");
            step.WithOwner().HasForeignKey("InvoiceId");
            step.HasKey(s => s.Id);

            step.Property(s => s.Sequence).IsRequired();
            step.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            step.Property(s => s.RejectionReason).HasMaxLength(1000);
        });
    }
}
