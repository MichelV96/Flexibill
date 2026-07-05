using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices.Events;

public sealed record InvoiceSubmittedForApprovalEvent(Guid InvoiceId, Guid OrganizationId, Guid BranchId)
    : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
