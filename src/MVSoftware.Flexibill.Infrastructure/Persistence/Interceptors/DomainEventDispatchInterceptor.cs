using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Infrastructure.Persistence.Outbox;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Transactional outbox (Technisch Ontwerp, hoofdstuk 3.3/6.3): schrijft de domain events van
/// alle getrackte <see cref="Entity"/>-instanties als <see cref="OutboxMessage"/>-rijen weg, in
/// dezelfde <c>SaveChangesAsync</c>-aanroep/transactie als de business-wijziging zelf
/// (<c>SavingChangesAsync</c>, dus vóór de commit - net als <see cref="AuditInterceptor"/>).
///
/// Publiceert zelf niets meer - dat doet <see cref="OutboxProcessor"/>, periodiek aangeroepen
/// vanuit de Worker (niet de Web App, hoofdstuk 12), zodat een falende/trage MediatR-handler
/// nooit de oorspronkelijke transactie kan laten mislukken en berichten een herstartbare Worker
/// overleven.
/// </summary>
public sealed class DomainEventDispatchInterceptor(IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            WriteOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteOutboxMessages(DbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker.Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                context.Set<OutboxMessage>().Add(OutboxMessage.Create(domainEvent, dateTimeProvider.UtcNow));
            }
        }
    }
}
