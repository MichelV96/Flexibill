namespace MVSoftware.Flexibill.Domain.Organizations;

/// <summary>
/// De drie losse modules uit het Functioneel Ontwerp, hoofdstuk 11 (Document Archive,
/// Expense Processing, Purchase Management). CRM Leveranciers en Factuurverwerking
/// horen bij de basis en zijn hier bewust geen aan/uit-zetbare module.
/// </summary>
[Flags]
public enum FlexibillModule
{
    None = 0,
    DocumentArchive = 1,
    ExpenseProcessing = 2,
    PurchaseManagement = 4
}
