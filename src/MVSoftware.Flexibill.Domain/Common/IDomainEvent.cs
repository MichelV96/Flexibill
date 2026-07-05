namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregates (e.g. InvoiceApprovedEvent).
/// Dispatched by the TransactionBehavior after a successful commit (Technisch Ontwerp, hoofdstuk 6.3).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
