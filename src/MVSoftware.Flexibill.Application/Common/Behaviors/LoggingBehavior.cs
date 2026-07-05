using MediatR;
using Microsoft.Extensions.Logging;

namespace MVSoftware.Flexibill.Application.Common.Behaviors;

/// <summary>
/// Structured logging van elk command/query (Technisch Ontwerp, hoofdstuk 6.3, stap 1).
/// TODO: aanvullen met de aanroepende Presentation-host (Web/Worker) zodra
/// ICurrentUserContext/ITenantContext zijn geïmplementeerd.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
