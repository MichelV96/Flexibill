using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Infrastructure.Persistence.Outbox;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

/// <summary>
/// Verwerkt onverwerkte <see cref="OutboxMessage"/>-rijen (Technisch Ontwerp, hoofdstuk 3.3/12) -
/// aangeroepen vanuit de Worker's <c>OutboxDispatcherFunction</c>, niet vanuit de Web App. Elk
/// bericht wordt gedeserialiseerd naar zijn concrete domain-event-type en via MediatR gepubliceerd
/// (dezelfde <see cref="DomainEventNotification{TDomainEvent}"/>-route als voorheen, alleen nu
/// asynchroon en persistent i.p.v. inline tijdens de oorspronkelijke SaveChanges).
/// </summary>
public sealed class OutboxProcessor(
    FlexibillDbContext dbContext,
    IPublisher publisher,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxProcessor> logger)
{
    private const int BatchSize = 50;

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = Type.GetType(message.Type)
                ?? throw new InvalidOperationException($"Cannot resolve outbox message type '{message.Type}'.");

            var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} deserialized to null.");

            await publisher.PublishDomainEventAsync(domainEvent, cancellationToken);

            message.MarkProcessed(dateTimeProvider.UtcNow);
        }
        catch (Exception ex)
        {
            // Eén kapot/onverwerkbaar bericht mag de rest van de batch niet blokkeren - blijft
            // onverwerkt staan (ProcessedOnUtc == null) en wordt bij de volgende poll opnieuw
            // geprobeerd. Geen max-aantal-pogingen/dead-letter in deze eerste versie.
            logger.LogError(ex, "Failed to process outbox message {MessageId} ({Type})", message.Id, message.Type);
            message.RecordError(ex.Message);
        }
    }
}
