using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Infrastructure.Authentication;
using MVSoftware.Flexibill.Infrastructure.Email;
using MVSoftware.Flexibill.Infrastructure.Persistence;
using MVSoftware.Flexibill.Infrastructure.Persistence.Interceptors;
using MVSoftware.Flexibill.Infrastructure.Persistence.Repositories;

namespace MVSoftware.Flexibill.Infrastructure;

/// <summary>
/// Registers the EF Core DbContext, Azure service clients (Blob Storage, Document
/// Intelligence, Communication Services, Key Vault), de zeven IAccountingConnector-
/// implementaties en MassTransit (Technisch Ontwerp, hoofdstuk 3.3, 10, 11, 13).
///
/// Gebruikt door zowel MVSoftware.Flexibill.Web als MVSoftware.Flexibill.Worker -
/// beide zijn gelijkwaardige "voorkanten" op dezelfde Application-laag (hoofdstuk 10.3).
///
/// De EF Core-persistence (DbContext, multi-tenancy/branch query filters, audit-trail, de
/// transactional outbox) staat er nu; login-flow (OTP) en e-mailverzending zijn nog TIJDELIJKE
/// in-memory/console-implementaties (zie hun eigen TODO's). Blob Storage-client, Document
/// Intelligence-client, accounting-connectors en de echte Service Bus-transport volgen in een
/// volgende stap.
///
/// LET OP: <see cref="ICurrentUserContext"/> wordt hier BEWUST niet geregistreerd - Web en
/// Worker hebben elk hun eigen implementatie nodig (<c>AuthenticationStateCurrentUserContext</c>
/// resp. <see cref="SystemCurrentUserContext"/>, Technisch Ontwerp hoofdstuk 6.3 punt 2) en
/// registreren die zelf in hun eigen <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFlexibillInfrastructure(this IServiceCollection services, string sqlConnectionString)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddDbContext<FlexibillDbContext>((serviceProvider, options) => options
            .UseSqlServer(sqlConnectionString)
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>()));
        services.AddScoped<OutboxProcessor>();

        services.AddSingleton<IOtpService, InMemoryOtpService>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IApprovalFlowSettingRepository, ApprovalFlowSettingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
