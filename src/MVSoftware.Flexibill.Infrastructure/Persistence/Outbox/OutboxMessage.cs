using System.Text.Json;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Outbox;

/// <summary>
/// Eén domain event, gepersisteerd door <see cref="Interceptors.DomainEventDispatchInterceptor"/>
/// in dezelfde transactie als de business-wijziging (transactional outbox). Wordt gelezen en
/// afgehandeld door <see cref="OutboxProcessor"/>, aangeroepen vanuit de Worker (Technisch
/// Ontwerp, hoofdstuk 3.3/12) - niet vanuit de Web App.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Assembly-qualified naam van het concrete domain-event-type, voor deserialisatie.</summary>
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(IDomainEvent domainEvent, DateTimeOffset occurredOnUtc)
    {
        var type = domainEvent.GetType();

        return new OutboxMessage
        {
            OccurredOnUtc = occurredOnUtc,
            Type = type.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type '{type}' has no assembly-qualified name."),
            Content = JsonSerializer.Serialize(domainEvent, type)
        };
    }

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void RecordError(string error) => Error = error;
}
