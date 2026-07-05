using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MVSoftware.Flexibill.Application.Common.Behaviors;

namespace MVSoftware.Flexibill.Application;

/// <summary>
/// Registers MediatR (commands/queries), FluentValidation and the pipeline
/// behaviors described in het Technisch Ontwerp, hoofdstuk 6.3:
/// Logging -> Authorization -> Validation -> Transaction -> Performance.
///
/// TODO (volgende stap): AuthorizationBehavior en PerformanceBehavior toevoegen -
/// die vergen resp. een IRequireRole-marker per command en zijn niet aan de EF Core
/// DbContext gekoppeld, dus apart opgepakt.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFlexibillApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}
