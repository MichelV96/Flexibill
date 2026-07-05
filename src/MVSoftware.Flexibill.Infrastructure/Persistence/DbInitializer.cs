using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Organizations;
using MVSoftware.Flexibill.Domain.Suppliers;
using MVSoftware.Flexibill.Domain.Users;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;
using MVSoftware.Flexibill.Infrastructure.Persistence.Interceptors;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

/// <summary>
/// Development-only: past migraties toe en seedt dezelfde demodata die eerder in de losse
/// InMemory*-repositories zat (organisatie, vestiging, twee gebruikers, vier leveranciers),
/// zodat de "Lokaal proberen"-flow uit het README blijft werken - nu tegen een echte
/// database.
///
/// Bouwt de <see cref="DbContextOptions{TContext}"/> en de interceptors bewust HANDMATIG op
/// (net als <see cref="FlexibillDbContextFactory"/>) i.p.v. ze uit de DI-container op te halen:
/// het opstarten gebeurt in een handmatig aangemaakte scope zonder HttpContext, en
/// `AddFlexibillInfrastructure` registreert <c>AuditInterceptor</c> zo dat die (indirect, via
/// <see cref="ICurrentUserContext"/>) de request-gebonden <c>AuthenticationStateCurrentUserContext</c>
/// nodig heeft - die gooit dan een exception, ook al gebruikt deze klasse zelf al een <see
/// cref="SystemCurrentUserContext"/> voor de DbContext-constructor. Door zelf te bouwen wordt
/// die hele DI-keten (en dus het HttpContext-probleem) vermeden (Technisch Ontwerp, hoofdstuk 18:
/// in productie lopen migraties als aparte pipeline-stap, sowieso zonder HttpContext).
/// </summary>
public static class DbInitializer
{
    public static async Task MigrateAndSeedAsync(string sqlConnectionString, CancellationToken cancellationToken)
    {
        var systemUser = new SystemCurrentUserContext();
        var dateTimeProvider = new SystemDateTimeProvider();

        var options = new DbContextOptionsBuilder<FlexibillDbContext>()
            .UseSqlServer(sqlConnectionString)
            .AddInterceptors(
                new AuditInterceptor(systemUser, new FixedAuditSourceProvider(AuditSource.Web), dateTimeProvider),
                new DomainEventDispatchInterceptor(dateTimeProvider))
            .Options;

        await using var context = new FlexibillDbContext(options, systemUser);

        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Organizations.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var organization = Organization.Create("Kantoorgroothandel Groep BV");
        organization.ActivateModule(FlexibillModule.DocumentArchive);
        organization.ActivateModule(FlexibillModule.ExpenseProcessing);
        context.Organizations.Add(organization);

        var branch = Branch.Create(organization.Id, "Amsterdam");
        branch.ClearDomainEvents(); // al "verwerkt" als onderdeel van de seed, geen dispatch nodig.
        context.Branches.Add(branch);

        var demoAdmin = User.Invite(
            organization.Id,
            EmailAddress.Of("demo@flexibill.nl"),
            "Demo Beheerder",
            roles: [UserRole.Administrator, UserRole.Approver, UserRole.SupplierManager],
            branchIds: [branch.Id]);
        demoAdmin.RecordLogin(DateTimeOffset.UtcNow.AddDays(-1));
        context.Users.Add(demoAdmin);

        // Fiatteerder ZONDER toegang tot de demo-vestiging - laat de waarschuwing uit
        // UC-B4/UC-B5 ("heeft geen toegang tot deze vestiging") zien in het fiateringsscherm.
        var tom = User.Invite(organization.Id, EmailAddress.Of("tom@flexibill.nl"), "Tom Bakker", [UserRole.Approver], []);
        context.Users.Add(tom);

        var draftSupplier = Supplier.CreateDraft(organization.Id, "Nieuwe Leverancier XYZ");
        draftSupplier.LinkToBranch(branch.Id);
        context.Suppliers.Add(draftSupplier);

        var incompleteSupplier = Supplier.CreateActive(
            organization.Id, "Schoonmaakdiensten Blink",
            chamberOfCommerceNumber: null, vatNumber: null, ibans: null,
            primaryContact: null, address: null, paymentTermDays: 30, category: "Diensten",
            defaultGeneralLedgerAccountId: null, defaultCostCenterId: null);
        incompleteSupplier.LinkToBranch(branch.Id);
        context.Suppliers.Add(incompleteSupplier);

        var completeSupplier = Supplier.CreateActive(
            organization.Id, "Kantoorgroothandel BV",
            ChamberOfCommerceNumber.Of("72345678"), VatNumber.Of("NL123456789B01"),
            [Iban.Of("NL91ABNA0417164300")],
            ContactPerson.Of("Jan de Vries"), address: null, paymentTermDays: 30, category: "Kantoorartikelen",
            defaultGeneralLedgerAccountId: Guid.NewGuid(), defaultCostCenterId: Guid.NewGuid());
        completeSupplier.LinkToBranch(branch.Id);
        context.Suppliers.Add(completeSupplier);

        var energySupplier = Supplier.CreateActive(
            organization.Id, "Grootzakelijk Energiecontract BV",
            ChamberOfCommerceNumber.Of("65432109"), VatNumber.Of("NL987654321B01"),
            [Iban.Of("NL02ABNA0123456789")],
            ContactPerson.Of("Klantenservice"), address: null, paymentTermDays: 14, category: "Energie",
            defaultGeneralLedgerAccountId: Guid.NewGuid(), defaultCostCenterId: Guid.NewGuid());
        energySupplier.LinkToBranch(branch.Id);
        context.Suppliers.Add(energySupplier);

        await context.SaveChangesAsync(cancellationToken);
    }
}
