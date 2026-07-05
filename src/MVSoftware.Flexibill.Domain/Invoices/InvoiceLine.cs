using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices;

/// <summary>
/// A single invoice line, coded to its own general ledger account, cost center and
/// VAT code (Technisch Ontwerp, hoofdstuk 5.4 - FO 6.3: codering gebeurt per regel,
/// niet op het totaal van de factuur).
/// </summary>
public sealed class InvoiceLine : Entity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money Amount { get; private set; } = Money.Zero();

    public Guid? GeneralLedgerAccountId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string? VatCode { get; private set; }

    /// <summary>
    /// Confidence score (0-1) reported by Azure AI Document Intelligence for this line
    /// (Technisch Ontwerp, hoofdstuk 10.2). Null for manually added lines.
    /// </summary>
    public decimal? OcrConfidence { get; private set; }

    public bool IsCoded => GeneralLedgerAccountId is not null && CostCenterId is not null && VatCode is not null;

    // Parameterloos nodig voor EF Core-materialisatie (Money/UnitPrice/Amount zijn owned types en
    // kunnen niet via constructor-binding gevuld worden) - zelfde patroon als ApprovalStep/
    // SupplierBranchLink/ApprovalFlowLevel. Geen gedragsverandering: alle members krijgen alsnog
    // hun waarde via het private field resp. de private setters.
    private InvoiceLine() { }

    private InvoiceLine(string description, decimal quantity, Money unitPrice, Money amount, decimal? ocrConfidence)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = amount;
        OcrConfidence = ocrConfidence;
    }

    internal static InvoiceLine Create(string description, decimal quantity, Money unitPrice, decimal? ocrConfidence = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("An invoice line requires a description.", nameof(description));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var amount = Money.Of(unitPrice.Amount * quantity, unitPrice.Currency);
        return new InvoiceLine(description, quantity, unitPrice, amount, ocrConfidence);
    }

    /// <summary>
    /// Applies the standard grootboekrekening/kostenplaats van de leverancier (FO 5.1) or an
    /// automatic OCR-based suggestion. Still leaves the line eligible for manual correction
    /// while the invoice is in status Coding.
    /// </summary>
    internal void ApplyCoding(Guid generalLedgerAccountId, Guid costCenterId, string vatCode)
    {
        if (string.IsNullOrWhiteSpace(vatCode))
        {
            throw new ArgumentException("A VAT code is required.", nameof(vatCode));
        }

        GeneralLedgerAccountId = generalLedgerAccountId;
        CostCenterId = costCenterId;
        VatCode = vatCode;
    }
}
