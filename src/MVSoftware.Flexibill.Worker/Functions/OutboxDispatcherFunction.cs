using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MVSoftware.Flexibill.Infrastructure.Persistence;

namespace MVSoftware.Flexibill.Worker.Functions;

/// <summary>
/// Leest en verwerkt de transactional outbox (Technisch Ontwerp, hoofdstuk 3.3/12) - de Web App
/// schrijft alleen naar de outbox (via <c>DomainEventDispatchInterceptor</c>), uitsluitend de
/// Worker haalt berichten op en publiceert ze via MediatR (<see cref="OutboxProcessor"/>).
/// </summary>
public class OutboxDispatcherFunction(OutboxProcessor outboxProcessor, ILogger<OutboxDispatcherFunction> logger)
{
    // Elke 30 seconden - vaak genoeg voor een responsief gevoel (bijv. de standaard-fiateringsflow
    // die na het aanmaken van een vestiging verschijnt), zonder de database te overbelasten.
    [Function(nameof(DispatchOutboxMessages))]
    public async Task DispatchOutboxMessages([TimerTrigger("*/30 * * * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking outbox for pending domain events");
        await outboxProcessor.ProcessPendingMessagesAsync(cancellationToken);
    }
}
