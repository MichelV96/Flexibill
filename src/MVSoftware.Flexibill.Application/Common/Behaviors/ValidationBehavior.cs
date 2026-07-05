using FluentValidation;
using MediatR;

namespace MVSoftware.Flexibill.Application.Common.Behaviors;

/// <summary>
/// Voert alle FluentValidation-validators voor het command/de query uit vóórdat de
/// handler draait (Technisch Ontwerp, hoofdstuk 6.2, 6.3 - stap 3 in de pipeline).
/// Gooit een ValidationException bij fouten; de Web-laag toont dit als een nette
/// foutmelding (zie ook Common/Result.cs voor het verwachte-fouten-pad daarnaast).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
