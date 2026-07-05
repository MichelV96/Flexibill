namespace MVSoftware.Flexibill.Contracts.Messages;

/// <summary>
/// Zie InvoiceApproved - het equivalent voor declaraties (hoofdstuk 12.1).
/// </summary>
public sealed record ExpenseApproved(
    Guid ExpenseId,
    Guid OrganizationId,
    Guid BranchId);
