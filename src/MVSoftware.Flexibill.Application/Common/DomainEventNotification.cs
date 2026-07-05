using MediatR;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Application.Common;

/// <summary>
/// Verpakt een domain event als MediatR-notificatie, zodat er losse
/// INotificationHandler-klassen op kunnen reageren (bijv. BranchCreatedEventHandler).
///
/// Gepubliceerd door <see cref="DomainEventDispatchExtensions.PublishDomainEventAsync"/>, aangeroepen
/// vanuit Infrastructure's <c>OutboxProcessor</c> - zelf aangeroepen vanuit de Worker, niet de Web
/// App (Technisch Ontwerp, hoofdstuk 3.3/12). `DomainEventDispatchInterceptor` schrijft events alleen
/// nog als outbox-rij weg; command handlers publiceren zelf nooit rechtstreeks.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
