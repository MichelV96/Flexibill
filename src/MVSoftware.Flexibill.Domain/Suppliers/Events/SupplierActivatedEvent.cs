using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Suppliers.Events;

/// <summary>
/// UC-C3: zodra een concept-leverancier wordt geactiveerd, luistert de Application-laag
/// hierop om gekoppelde facturen met status AwaitingSupplierApproval door te laten
/// stromen naar Coding (Invoice.OnSupplierActivated, UC-F7).
/// </summary>
public sealed record SupplierActivatedEvent(Guid SupplierId, Guid OrganizationId) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
