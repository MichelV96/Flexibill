using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices.Events;

/// <summary>
/// Gepubliceerd zodra de laatste vereiste ApprovalStep is goedgekeurd. Triggert, via de
/// outbox en Service Bus, de export naar het boekhoudpakket (Technisch Ontwerp, hoofdstuk 12.1).
/// </summary>
public sealed record InvoiceApprovedEvent(Guid InvoiceId, Guid OrganizationId, Guid BranchId) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
