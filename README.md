# Flexibill

Inkoopfactuurverwerkingsplatform. Zie de twee ontwerpdocumenten voor de volledige context:

- `Functioneel-ontwerp-Inkoopfactuurverwerking.docx` (v1.1)
- `Technisch-ontwerp-Flexibill.docx` (v0.2)

## Projectstructuur (Clean Architecture)

`.NET 10` gebruikt standaard het nieuwe `Flexibill.slnx`-formaat (in plaats van `.sln`) - open of build dit
gewoon met `dotnet build Flexibill.slnx`, dat werkt identiek.

```
src/
  MVSoftware.Flexibill.Domain/          Entities, value objects, domain events. Geen dependencies.
  MVSoftware.Flexibill.Application/     CQRS-commands/queries (MediatR), validatie (FluentValidation).
  MVSoftware.Flexibill.Infrastructure/  EF Core, Azure-clients, accounting-connectors, MassTransit.
  MVSoftware.Flexibill.Web/             Blazor Web App (Static SSR + Interactive Server) + Minimal API's.
  MVSoftware.Flexibill.Worker/          Azure Functions (isolated worker): OCR, exports, notificaties, timers.
  MVSoftware.Flexibill.Contracts/       Gedeelde Service Bus message-contracten (Web <-> Worker).
tests/
  MVSoftware.Flexibill.Domain.Tests/
  MVSoftware.Flexibill.Application.Tests/
  MVSoftware.Flexibill.Infrastructure.IntegrationTests/   (Testcontainers, echte SQL Server-container)
  MVSoftware.Flexibill.Architecture.Tests/                (NetArchTest: afdwingen laag-regels)
  MVSoftware.Flexibill.Web.EndToEndTests/                 (Playwright)
```

Zowel `Web` als `Worker` refereren naar **zowel** `Application` **als** `Infrastructure` — beide zijn
gelijkwaardige "voorkanten" die schrijven via de Application-laag (zie Technisch Ontwerp, hoofdstuk 10.3).

## Belangrijk: NuGet-restore en lokale database

`dotnet restore`/`dotnet build` werken inmiddels (NuGet-toegang is niet langer een blokkade). Werk de
pakketversies wel nog bij naar de daadwerkelijk laatste stabiele versies (`dotnet list package --outdated`)
voordat je verder bouwt.

De Web App en de Worker verwachten een lokale **SQL Server LocalDB** (`(localdb)\mssqllocaldb`) - zie
`appsettings.Development.json` resp. `local.settings.json` voor de connection string. Migraties worden bij
het opstarten van de Web App in `Development` automatisch toegepast + geseed (`DbInitializer`, zie hieronder);
in productie lopen migraties als losse pipeline-stap (Technisch Ontwerp, hoofdstuk 18).

## Wat is er al ingevuld

- **EF Core-persistence, multi-tenancy, audit-trail en transactional outbox (Technisch Ontwerp hoofdstuk
  3.1, 3.3, 4, 6.3, 12, 15)** - vervangt alle vijf `InMemory*`-repositories en het handmatige
  `PublishDomainEventsAsync`-mechanisme:
  - `Infrastructure/Persistence/FlexibillDbContext.cs` - `DbSet`s voor alle zes aggregates +
    `AuditLogEntry` + `OutboxMessage`; `OnModelCreating` past via reflectie op elk `ITenantEntity` een
    benoemd `"Tenant"`-queryfilter toe en op elk `IBranchScopedEntity` een `"Branch"`-filter (EF Core
    combineert benoemde filters automatisch met AND); `Supplier`s n-op-n branch-zichtbaarheid krijgt een
    eigen filter (`BranchLinks.Any(...)`).
  - `Infrastructure/Persistence/Configurations/` - een `IEntityTypeConfiguration<T>` per aggregate; value
    objects met één scalar (`EmailAddress`/`Iban`/`ChamberOfCommerceNumber`/`VatNumber`) via `HasConversion`,
    meervoudige (`Money`/`Address`/`ContactPerson`) via `OwnsOne`, child-entities
    (`InvoiceLine`/`ApprovalStep`/`SupplierBranchLink`/`ApprovalFlowLevel`) via `OwnsMany`.
    `User.Roles`/`BranchIds` (collecties van primitieven achter een read-only property) via een
    value-converter naar een comma-separated kolom.
  - **Transactional outbox (eigen, lichtgewicht tabel - géén MassTransit)**:
    `Infrastructure/Persistence/Interceptors/DomainEventDispatchInterceptor.cs` schrijft, vóór elke commit
    (`SavingChangesAsync`), de domain events van alle getrackte aggregates weg als `OutboxMessage`-rijen
    (`Infrastructure/Persistence/Outbox/OutboxMessage.cs`) - in dezelfde transactie als de business-
    wijziging, dus atomisch, maar zonder zelf iets te publiceren. `Infrastructure/Persistence/
    OutboxProcessor.cs` leest onverwerkte berichten, deserialiseert ze terug naar hun concrete
    domain-event-type en publiceert ze alsnog via MediatR (`DomainEventNotification<T>`) - **uitsluitend
    aangeroepen vanuit de Worker** (`Worker/Functions/OutboxDispatcherFunction.cs`, elke 30 seconden), nooit
    vanuit de Web App. Gevolg: event-afhandeling (bijv. de standaard-fiateringsflow na het aanmaken van een
    vestiging) is nu asynchroon i.p.v. binnen hetzelfde request. Geen dead-lettering/max-pogingen in deze
    eerste versie - een falend bericht blijft onverwerkt staan en wordt bij de volgende poll opnieuw
    geprobeerd.
  - Omdat de Worker nu zelf de DbContext gebruikt (voor de outbox), heeft die een eigen
    `ICurrentUserContext` nodig - `SystemCurrentUserContext` (Technisch Ontwerp hoofdstuk 6.3 punt 2,
    "SystemPrincipal"), i.p.v. de request-gebonden `HttpContextCurrentUserContext` die alleen de Web App
    gebruikt. Beide hosts registreren dit voortaan zelf in hun eigen `Program.cs` (niet meer in
    `AddFlexibillInfrastructure`).
  - `AuditInterceptor.cs` - legt wijzigingen aan `IAuditable`-aggregates vast in de `AuditLog`-tabel, in
    dezelfde transactie (bekende beperking: alleen op aggregate-root-niveau, nog niet voor wijzigingen die
    uitsluitend een owned child-collectie raken).
  - `Application/Common/Behaviors/TransactionBehavior.cs` (+ `IUnitOfWork`) - omvat de hele command-uitvoering
    in één EF Core-transactie (commands met meerdere repository-writes moeten atomisch zijn, inclusief hun
    outbox-rijen).
  - `Infrastructure/Persistence/Repositories/` - `OrganizationRepository`/`BranchRepository`/
    `UserRepository`/`SupplierRepository`/`ApprovalFlowSettingRepository` (géén `Ef`-prefix, eigen mapje
    voor alle repository-implementaties) - vervangen de gelijknamige `InMemory*`-klassen 1-op-1 (zelfde
    Application-interfaces, geen wijziging aan command/query handlers nodig, op `CreateBranchCommandHandler`
    na - die publiceert nooit meer rechtstreeks, dat gaat nu altijd via de outbox).
  - `FlexibillDbContextFactory` (design-time) + migraties `InitialCreate` en `AddOutboxMessages`;
    `DbInitializer` past bij het opstarten van de Web App in `Development` migraties toe en seedt dezelfde
    demodata die eerder in de `InMemory*`-constructors zat.
  - **Nog niet gedaan**: `Infrastructure.IntegrationTests` (Testcontainers) voor de query filters/interceptors
    - bewust overgeslagen, zie "Volgende stappen".
- Solution + alle 11 projecten, met project-references die de Clean Architecture-laagregels volgen.
- `Domain/Common/`: `Entity`, `ValueObject`, `Money`, `Iban`, `ChamberOfCommerceNumber`, `VatNumber`,
  `EmailAddress`, `Address`, `ITenantEntity`, `IBranchScopedEntity`, `IAuditable`, `IDomainEvent`,
  `DomainException`.
- **`Domain/Invoices/`**: de volledige `Invoice`-aggregate met de statusmachine (hoofdstuk 5.3),
  regelcodering (`InvoiceLine`) en approval-afhandeling (`ApprovalStep`, standaard- én uitzonderingsflow).
- **`Domain/Suppliers/`**: de `Supplier`-aggregate (concept/actief, FO 5.2), vestigingskoppelingen,
  en `GetMissingRequiredFields()` t.b.v. het dashboard-signaal (UC-C6).
- **`Domain/Organizations/`**: de `Organization`-aggregate - welke losse modules actief zijn
  (`FlexibillModule`, FO hoofdstuk 11) en de abonnementsvorm (FO hoofdstuk 12).
- **`Domain/Branches/`**: de `Branch`-aggregate - naam/adres en de boekhoudkoppeling (FO 6.6); publiceert
  `BranchCreatedEvent` (de bijbehorende `ApprovalFlowSetting` bestaat inmiddels, zie hieronder - het
  daadwerkelijk aanroepen vanuit een event-handler is de volgende stap).
- **`Domain/Approvals/`**: de `ApprovalFlowSetting`-aggregate (FO 6.4) - de standaardflow van een vestiging
  (1 niveau, automatisch aangemaakt) én leverancier-uitzonderingen (meerdere niveaus, vast/roulerend,
  volgtijdelijk/parallel), met bedragsgrenzen per niveau. `ResolveRequiredApprovers(Money)` sluit
  rechtstreeks aan op `Invoice.SubmitForApproval` - zie de integratietests die beide aggregates samen
  laten werken (`ApprovalFlowSettingInvoiceIntegrationTests`).
- **`Domain/Users/`**: minimale `User`-aggregate (rollen zijn combineerbaar, FO 3.4).
- Alle zes aggregates hebben unit tests onder `tests/MVSoftware.Flexibill.Domain.Tests/` (63 tests totaal).
  **`Domain` blijft foutloos en waarschuwingsvrij bouwen** in deze omgeving (geverifieerd na elke stap).
- **Fiateringsflow configureren, end-to-end (UC-B2/UC-B4/UC-B5)**:
  - `Application/Common/DomainEventNotification.cs` + `DomainEventDispatchExtensions.cs` - een
    TIJDELIJK mechanisme om domain events als MediatR-notificaties te publiceren (`IPublisher.
    PublishDomainEventsAsync(entity, ct)`), vooruitlopend op de echte transactional outbox
    (Technisch Ontwerp, hoofdstuk 3.3/6.3) die er komt zodra de EF Core `DbContext` bestaat.
  - `Branches/Commands/CreateBranchCommand.cs` (UC-B2) publiceert `BranchCreatedEvent`, opgepakt door
    `Branches/EventHandlers/BranchCreatedEventHandler.cs` die automatisch de simpele standaardflow
    aanmaakt (`ApprovalFlowSetting.CreateDefaultStandardFlow`) - precies de FO 6.4-regel "nieuwe
    vestiging krijgt automatisch de standaardflow".
  - `Approvals/Commands/ConfigureApprovalFlowCommand.cs` (UC-B4 én UC-B5, met `SupplierId` als
    onderscheid) - inclusief de cross-aggregate validatie uit Technisch Ontwerp hoofdstuk 4.2: elke
    toegewezen Fiatteerder moet bestaan, de rol Approver hebben, én toegang hebben tot de vestiging.
    Bewust in de Application-laag, niet in de `ApprovalFlowSetting`-aggregate zelf.
  - Getest met NSubstitute-mocks in `tests/MVSoftware.Flexibill.Application.Tests/Approvals/` (6 tests,
    incl. de twee weigeringspaden: geen vestigingstoegang, en geen Approver-rol).
  - Nieuwe in-memory repositories: `InMemoryBranchRepository` (vestiging "Amsterdam") en
    `InMemoryApprovalFlowSettingRepository`. `InMemoryUserRepository` heeft er een tweede demo-gebruiker
    bij (**tom@flexibill.nl**, Fiatteerder zónder vestigingstoegang) specifiek om het weigeringspad te
    kunnen laten zien.
- **Gebruikersbeheer, volledig inclusief scherm (FO hoofdstuk 4.3, UC-B1)**:
  - `Application/Users/Commands/`: `InviteUserCommand`, `UpdateUserCommand` (rollen/vestigingen wijzigen),
    `SetUserActiveStatusCommand` (deactiveren/heractiveren i.p.v. verwijderen, FO 4.3 - met een ingebouwde
    veiligheidsregel: je kunt jezelf niet deactiveren).
  - `Application/Users/Queries/GetUsersOverviewQuery.cs` en `Application/Branches/Queries/
    GetBranchesOverviewQuery.cs` (voor de vestiging-dropdown).
  - `IUserRepository`/`IBranchRepository` uitgebreid met echte schrijf-/lijstmethoden;
    `InMemoryUserRepository` is nu een muteerbare, thread-veilige store (`ConcurrentDictionary`) in
    plaats van een statische seed-lijst.
  - `Web/Components/Pages/Administration/Users.razor` (route `/administration/users`,
    `[Authorize(Roles = "Administrator")]`) - **Interactive Server**: overzichtstabel met rollen/
    vestigingen/laatste login/status, een in-/uitklapbaar formulier voor zowel uitnodigen als bewerken
    (rollen en vestigingen als MudBlazor multi-select), en direct deactiveren/activeren per rij.
  - Navigatielink in `MainLayout.razor`, alleen zichtbaar voor Administrators (`<AuthorizeView Roles=
    "Administrator">`).
  - `Domain/Users/User.cs` uitgebreid met `Activate()` en `UpdateRolesAndBranches(...)`.
- **Fiateringsflow instellen, het scherm (FO 6.4, UC-B4/UC-B5)**:
  - `Application/Approvals/Queries/GetApprovalFlowOverviewQuery.cs` - haalt de standaardflow én alle
    leverancier-uitzonderingen van een vestiging op, met namen (i.p.v. kale id's) en per niveau een
    `ApproverLacksBranchAccess`-vlag - dat is dezelfde waarschuwing als in `ConfigureApprovalFlowCommand`,
    nu ook zichtbaar vóórdat je iets opslaat, niet pas erna.
  - `Application/Approvals/Commands/DeleteSupplierExceptionCommand.cs` - een uitzondering verwijderen
    (de leverancier valt daarna terug op de standaardflow).
  - `ISupplierRepository`/`IApprovalFlowSettingRepository` verder uitgebreid (`GetByBranchAsync`,
    `GetSupplierExceptionsForBranchAsync`, `DeleteAsync`); `InMemorySupplierRepository` heeft er een
    vierde demo-leverancier bij (**Grootzakelijk Energiecontract BV**) - precies de leverancier uit het
    FO-voorbeeld voor de uitzonderingsflow.
  - `Web/Components/Pages/Administration/ApprovalFlow.razor` (route `/administration/approval-flow`,
    alleen Administrators) - **Interactive Server**: standaardflow bewerken (niveaus met
    fiatteerder-keuze, "elke fiatteerder" als optie, bedragsgrens per niveau, volgtijdelijk/parallel-
    schakelaar), plus leverancier-uitzonderingen toevoegen/bewerken/verwijderen. De waarschuwing bij een
    fiatteerder zonder vestigingstoegang staat er als icoon bij het niveau, exact zoals in de eerdere
    wireframe.
  - Herbruikbaar `ApprovalFlowLevelsEditor.razor`-component, gedeeld tussen de standaardflow en elke
    leverancier-uitzondering (dezelfde niveaus-UI, geen duplicatie).
  - Navigatielink "Fiatering" toegevoegd in `MainLayout.razor`.
  - Nieuwe Application-tests: `GetApprovalFlowOverviewQueryHandlerTests` en
    `DeleteSupplierExceptionCommandHandlerTests`.
- **Login-flow (OTP), end-to-end**: zie vorige versie van dit bestand voor de details - `RequestOtpCommand`
  /`ValidateOtpCommand`, MudBlazor-pagina's `/login` en `/login/verify`, cookie-authenticatie.
- **Dashboard, rolafhankelijk (FO hoofdstuk 20)**:
  - `Application/Common/Interfaces/ICurrentUserContext.cs` - de ingelogde gebruiker (rollen, vestigingen)
    afgeleid uit de claims (hoofdstuk 4.1-4.3, 9.1).
  - `Application/Dashboard/Queries/GetDashboardQuery.cs` - bouwt de dashboardkaarten op basis van rol;
    kaarten die nog niet aan een repository gekoppeld kunnen worden (mislukte exports, openstaande
    fiatteringen, ...) staan expliciet op `null` met een TODO, in plaats van een misleidende "0".
  - `Infrastructure/Authentication/HttpContextCurrentUserContext.cs` - de ingelogde gebruiker afgeleid uit
    de claims; `IOrganizationRepository`/`ISupplierRepository` zijn inmiddels EF Core-implementaties (zie
    de EF Core-persistence-bullet hierboven), destijds nog in-memory.
  - `Web/Components/Pages/Home.razor` (route `/`) - **Interactive Server** (i.t.t. de Login-pagina's:
    hier is straks wél live bijwerken gewenst, FO/TO 7.1) met MudBlazor-kaarten
    (`Components/Pages/Dashboard/DashboardCard.razor`), plus `[Authorize]` en `AuthorizeRouteView` +
    `RedirectToLogin` in `Routes.razor` zodat niet-ingelogde bezoekers naar `/login` gaan.

## Volgende stappen

1. `Infrastructure.IntegrationTests` met Testcontainers (SQL Server-container) voor de multi-tenancy-/
   branch-queryfilters, de `AuditInterceptor` en de outbox (`DomainEventDispatchInterceptor` +
   `OutboxProcessor`) - bewust nog niet gedaan.
2. `Invoice`-use cases aan de Application-laag knopen: `SubmitInvoiceForApprovalCommand` die
   `ApprovalFlowSetting.ResolveRequiredApprovers` gebruikt, plus `ApproveInvoiceCommand`/`RejectInvoiceCommand`
   en de bijbehorende schermen (facturenoverzicht, factuurverwerking, fiattering).
3. Uitnodigingsmail daadwerkelijk versturen vanuit `InviteUserCommand` (nu alleen een TODO); een
   acceptatie-/eerste-login-ervaring voor nieuw uitgenodigde gebruikers.
4. Een vestigingenoverzicht/-beheerscherm (`CreateBranchCommand` bestaat al, maar heeft nog geen UI) -
   nu kiest `ApprovalFlow.razor` bij ontbrekend `branchId`-queryparameter automatisch de eerste vestiging.
5. `AuthorizationBehavior`/`PerformanceBehavior` (Technisch Ontwerp hoofdstuk 6.3) - vergen een
   `IRequireRole`-marker per command resp. staan los van de EF Core-laag, bewust apart gehouden.
6. De overige use cases: boekhoudkoppeling-connectors, OCR-verwerking, declaraties, inkoopmanagement.

## Lokaal proberen

Vereist een lokale SQL Server LocalDB (standaard aanwezig bij Visual Studio op Windows). Start
`MVSoftware.Flexibill.Web` (`dotnet run`) - migraties + demodata worden automatisch toegepast bij het
opstarten in `Development`.

- Inloggen met **demo@flexibill.nl** (Administrator + Approver) of **tom@flexibill.nl** (alleen Approver,
  geen vestigingstoegang - handig om het weigeringspad in het fiateringsscherm te zien).
- Dashboard (`/`), Gebruikers (`/administration/users`), Fiatering (`/administration/approval-flow`).
