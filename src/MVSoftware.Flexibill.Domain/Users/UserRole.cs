namespace MVSoftware.Flexibill.Domain.Users;

/// <summary>
/// Technisch Ontwerp, hoofdstuk 1.2 (vertaaltabel). Rollen zijn combineerbaar
/// (Functioneel Ontwerp, hoofdstuk 3.4) - een User heeft daarom een verzameling
/// van deze waarden, nooit precies één.
/// </summary>
public enum UserRole
{
    Administrator,
    SupplierViewer,
    SupplierManager,
    Approver,
    ExpenseSubmitter,
    ExpenseApprover,
    DocumentViewer,
    PurchaseRequester,
    PurchaseApprover
}
