using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVSoftware.Flexibill.Application;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Infrastructure;
using MVSoftware.Flexibill.Infrastructure.Persistence;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Registers MediatR, FluentValidation and the pipeline behaviors (see MVSoftware.Flexibill.Application).
        services.AddFlexibillApplication();

        // Registers the DbContext, Azure service clients, accounting connectors and MassTransit (see MVSoftware.Flexibill.Infrastructure).
        // The Worker uses exactly the same registrations as the Web app - it is a second, equally valid
        // "front door" onto the Application layer (see Technisch Ontwerp, hoofdstuk 10.3).
        var sqlConnectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("Setting 'SqlConnectionString' is not configured.");
        services.AddFlexibillInfrastructure(sqlConnectionString);
        services.AddSingleton<IAuditSourceProvider>(new FixedAuditSourceProvider(AuditSource.Worker));

        // De Worker heeft geen HttpContext - "SystemPrincipal" i.p.v. AuthenticationStateCurrentUserContext
        // (Technisch Ontwerp, hoofdstuk 6.3 punt 2). Zonder specifieke tenant: prima voor
        // tenant-onafhankelijk werk zoals de outbox-verwerking hieronder.
        services.AddScoped<ICurrentUserContext>(_ => new SystemCurrentUserContext());
    })
    .Build();

host.Run();
