using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices.Events;

public sealed record InvoiceProcessedEvent(Guid InvoiceId, Guid OrganizationId, Guid BranchId, string ExternalBookingReference)
    : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
