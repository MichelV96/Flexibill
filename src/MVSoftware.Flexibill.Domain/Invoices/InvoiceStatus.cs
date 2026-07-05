namespace MVSoftware.Flexibill.Domain.Invoices;

/// <summary>
/// Technisch Ontwerp, hoofdstuk 5.3:
/// New -> (AwaitingSupplierApproval) -> Coding -> PendingApproval
///      -> Approved -> Processed -> Archived
///                   -> Rejected -> (terug naar Coding)
/// </summary>
public enum InvoiceStatus
{
    New,
    AwaitingSupplierApproval,
    Coding,
    PendingApproval,
    Approved,
    Rejected,
    Processed,
    Archived
}
