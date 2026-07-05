using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices.Events;

public sealed record InvoiceRejectedEvent(Guid InvoiceId, Guid OrganizationId, Guid BranchId, string Reason)
    : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
