using MediatR;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Application.Common;

public static class DomainEventDispatchExtensions
{
    /// <summary>
    /// Verpakt <paramref name="domainEvent"/> in een <see cref="DomainEventNotification{TDomainEvent}"/>
    /// en publiceert die via MediatR. Gebruikt door Infrastructure's <c>OutboxProcessor</c> nadat een
    /// outbox-bericht is gedeserialiseerd (Technisch Ontwerp, hoofdstuk 3.3/12).
    /// </summary>
    public static Task PublishDomainEventAsync(this IPublisher publisher, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
        return publisher.Publish(notification, cancellationToken);
    }
}
