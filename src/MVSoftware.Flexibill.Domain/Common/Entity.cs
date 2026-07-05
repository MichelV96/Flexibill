namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Base class for entities. Carries domain events that are dispatched after
/// SaveChanges via the transactional outbox (Technisch Ontwerp, hoofdstuk 3.3).
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
