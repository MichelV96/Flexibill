using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Common.Behaviors;

/// <summary>
/// Opent een EF Core-transactie rond de handler (Technisch Ontwerp, hoofdstuk 6.3, stap 4).
/// Zorgt dat een command handler die meerdere repository-aanroepen (dus meerdere
/// <c>SaveChangesAsync</c>-calls) doet, atomisch is - inclusief de outbox-rijen die de
/// domain-event-dispatch-interceptor (Infrastructure) daarbij wegschrijft: mislukt een latere
/// stap, dan draait de hele transactie terug, ook de al geschreven outbox-berichten.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
