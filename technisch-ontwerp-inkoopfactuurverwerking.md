# Technisch Ontwerp – Flexibill

**Versie:** 0.2 (concept)
**Datum:** 3 juli 2026
**Gebaseerd op:** Functioneel Ontwerp v1.1
**Status:** Opgemaakt als Word-document

---

## 1. Inleiding en uitgangspunten

Dit document beschrijft de technische architectuur voor **Flexibill**, het inkoopfactuurverwerkingsplatform uit het functioneel ontwerp. Uitgangspunten:

- **.NET 10 (LTS)** — zie hoofdstuk 21.1.
- **Blazor Web App**, met een mix van **Static SSR** (merendeel van de schermen) en **Interactive Server** (gericht ingezet: fiattering, dashboard, en andere plekken met live updates).
- **Minimal API's** voor programmatische endpoints (hybride mobiele app, integraties).
- **Clean Architecture**: Domain, Application, Infrastructure, Presentation. De Application-laag is de businesslogicalaag: zij verzorgt validatie én de daadwerkelijke actie richting de database. Zowel de Web App als de Function App zijn gelijkwaardige "voorkanten" die deze laag aanroepen (zie hoofdstuk 3 en 10.3).
- **Azure-focus**: Azure App Service, Azure Functions voor achtergrondtaken, Azure Service Bus voor messaging, Azure AI Document Intelligence (OCR) voor het uitlezen van documenten, Azure Blob Storage voor het documentarchief.
- **Azure SQL Database** als operationele database, **één gedeelde database** met een tenant-kolom (`OrganizationId`) en EF Core *global query filters* voor multi-tenancy.

### 1.1 Naamgeving
- Productnaam: **Flexibill**.
- Root-namespace voor alle projecten: **`MVSoftware.Flexibill`** (bijv. `MVSoftware.Flexibill.Domain`, `MVSoftware.Flexibill.Application`).
- Alle types, members en methodenamen in de code zijn **Engelstalig**, ook waar het functioneel ontwerp Nederlandse termen gebruikt. Dit document hanteert daarom een vaste vertaaltabel (1.2) en gebruikt vanaf hier de Engelse termen voor alles wat code wordt; de Nederlandse FO-term wordt waar nuttig tussen haakjes vermeld voor de traceerbaarheid.

### 1.2 Vertaaltabel FO-termen → code-termen
| FO-term (NL) | Code-term (EN) |
|---|---|
| Administratie | `Organization` |
| Vestiging | `Branch` |
| Gebruiker | `User` |
| Rol | `Role` |
| Leverancier (concept/actief) | `Supplier` (status `Draft` / `Active`) |
| Factuur | `Invoice` |
| Factuurregel | `InvoiceLine` |
| Fiatteren / Fiattering | `Approve` / `Approval` |
| Fiatteringsstap | `ApprovalStep` |
| Fiateringsflow(instelling) | `ApprovalFlowSetting` |
| Boekhoudkoppeling | `AccountingConnection` |
| Declaratie | `Expense` |
| Inkooporder / -regel | `PurchaseOrder` / `PurchaseOrderLine` |
| Contract | `Contract` |
| Kostenplaats | `CostCenter` |
| Grootboekrekening | `GeneralLedgerAccount` |
| Btw-code | `VatCode` |
| KVK-nummer | `ChamberOfCommerceNumber` |
| Btw-nummer | `VatNumber` |
| Beheerder (rol) | `Administrator` |
| Leverancier lezer | `SupplierViewer` |
| Leverancier beheerder | `SupplierManager` |
| Fiatteerder | `Approver` |
| Declarant | `ExpenseSubmitter` |
| Declaratie-goedkeurder | `ExpenseApprover` |
| Documentlezer | `DocumentViewer` |
| Inkoop aanvrager | `PurchaseRequester` |
| Inkoop goedkeurder | `PurchaseApprover` |

---

## 2. Architectuuroverzicht (Azure)

Op hoofdlijnen bestaat de oplossing uit vijf draaiende onderdelen:

1. **Web App** (Azure App Service) — het Blazor Web App-proces: bevat zowel de Blazor-UI (Static SSR + Interactive Server) als de Minimal API-endpoints, in één ASP.NET Core host.
2. **Function App** (Azure Functions, isolated worker, .NET 10) — achtergrondverwerking: OCR-aanroepen, verwerking naar boekhoudpakketten, notificaties versturen, retentie-/signaleringstaken (timers). Draait bovenop **dezelfde Application- en Infrastructure-laag** als de Web App (zie hoofdstuk 3.1 en 10.3) en schrijft dus net zo goed rechtstreeks naar de database.
3. **Azure SQL Database** — alle operationele data (multi-tenant, gedeeld), met **point-in-time restore** als back-upstrategie (één regio, geen failover-groep, zie hoofdstuk 21.3).
4. **Azure Blob Storage** — documentarchief (facturen, declaratiebonnen, contracten, overige documenten).
5. **Azure Service Bus** — asynchrone communicatie/taakverdeling tussen Web App en Function App (queues en topics, zie hoofdstuk 12).

Daaromheen:
- **Azure AI Document Intelligence** — OCR/factuurherkenning (prebuilt invoice-model, zie hoofdstuk 10).
- **Azure SQL Full-Text Search** — volledige-tekst zoeken in het documentarchief voor v1 (Azure AI Search is een toekomstige uitbreiding, zie hoofdstuk 11.3 en 21.7).
- **Azure Communication Services (e-mail)** — verzenden van OTP-codes en notificatie-e-mails.
- **Azure Key Vault** — geheimen, connection strings, OAuth-tokens van boekhoudkoppelingen.
- **Application Insights** — logging, tracing en monitoring over alle onderdelen heen.

**Diagram: Azure-architectuur op hoofdlijnen**

![Azure-architectuur van Flexibill](27_architectuur_azure.png)

Zowel de Web App als de Function App schrijven rechtstreeks naar Azure SQL Database en Blob Storage — beide via dezelfde Application-laag (zie hoofdstuk 3 en 10.3) — en wisselen onderling berichten uit via Service Bus.

---

## 3. Clean Architecture — lagen en projectstructuur

### 3.1 Lagen
- **Domain** (`MVSoftware.Flexibill.Domain`) — entities, value objects, domain events, domain-interfaces (bijv. `IInvoiceRepository`). Geen dependencies op andere lagen of op NuGet-pakketten buiten de BCL.
- **Application** (`MVSoftware.Flexibill.Application`) — dit is de **businesslogicalaag**. Alle use cases (CQRS-commands/queries via MediatR) zitten hier, inclusief validatie (FluentValidation) én de daadwerkelijke actie richting de database (via repositories/`DbContext`, aangeroepen vanuit de command handlers). Afhankelijk van Domain, niet van Infrastructure of Presentation — Infrastructure implementeert alleen de interfaces die Application definieert.
- **Infrastructure** (`MVSoftware.Flexibill.Infrastructure`) — implementaties van de Application-interfaces: EF Core `DbContext` + repositories, Azure Blob Storage-client, Azure AI Document Intelligence-client, boekhoudkoppeling-connectors, Service Bus-integratie (via MassTransit, zie 3.3), OTP/e-mailverzending.
- **Presentation** — **elke manier waarop een verzoek de Application-laag bereikt**: de Web App (Blazor-componenten + Minimal API-endpoints) én de Function App (Service Bus-triggers, timer-triggers) zijn hierin **gelijkwaardig**. Beide roepen uitsluitend MediatR-commands/queries aan; welke "voorkant" een command aanroept (een HTTP-request of een Service Bus-bericht) doet er voor de businesslogica niet toe — zie 10.3 voor de expliciete onderbouwing van deze keuze.

**Diagram: lagen en afhankelijkheidsrichting**

![Clean Architecture-lagen van Flexibill](28_clean_architecture_lagen.png)

Presentation en Infrastructure hangen beide af van Application; Application hangt af van Domain. Er loopt nooit een afhankelijkheid de andere kant op — dit wordt afgedwongen door de architectuurtests (3.4).

### 3.2 Projectstructuur

```
src/
  MVSoftware.Flexibill.Domain/
  MVSoftware.Flexibill.Application/
  MVSoftware.Flexibill.Infrastructure/
  MVSoftware.Flexibill.Web/                 (Blazor Web App + Minimal API's, App Service)
  MVSoftware.Flexibill.Worker/              (Azure Functions isolated, achtergrondverwerking)
  MVSoftware.Flexibill.Contracts/           (gedeelde Service Bus message-contracten)
tests/
  MVSoftware.Flexibill.Domain.Tests/
  MVSoftware.Flexibill.Application.Tests/
  MVSoftware.Flexibill.Infrastructure.IntegrationTests/
  MVSoftware.Flexibill.Architecture.Tests/  (NetArchTest: afdwingen laag-regels)
  MVSoftware.Flexibill.Web.EndToEndTests/   (Playwright)
```

Zowel `MVSoftware.Flexibill.Web` als `MVSoftware.Flexibill.Worker` refereren naar **zowel** `Application` **als** `Infrastructure` (voor dependency injection van de concrete implementaties) — dit is precies waarom beide naar de database mogen en kunnen schrijven: ze delen dezelfde Application-laag, alleen de aanroep-trigger verschilt.

`MVSoftware.Flexibill.Contracts` bevat alleen de message-DTO's die over Service Bus gaan (bijv. `InvoiceOcrRequested`), gebruikt door zowel Web (publiceren) als Worker (consumeren).

### 3.3 Messaging-technologie
Voor Service Bus wordt **MassTransit** gebruikt in plaats van de kale Azure.Messaging.ServiceBus-SDK. Redenen:
- Ingebouwde **transactional outbox** (in combinatie met EF Core) — voorkomt dat een bericht verstuurd wordt terwijl de bijbehorende databasewijziging niet doorgaat (of andersom). Dit geldt voor beide hosts (Web én Worker), want beide voeren transacties uit tegen dezelfde database.
- Ingebouwde retry-/circuit breaker-policies en dead-lettering (via MassTransit's ingebouwde `_error`-queues — er is dus geen losse, handmatig gemodelleerde "mislukt"-queue nodig, zie hoofdstuk 12).
- Eenvoudiger te testen (in-memory transport voor unit-/integratietests).

### 3.4 Architectuurregels afdwingen
`MVSoftware.Flexibill.Architecture.Tests` bevat NetArchTest-regels die falen als bijvoorbeeld Domain een dependency krijgt op Infrastructure, of als een Presentation-project (Web of Worker) een MediatR-command *overslaat* en rechtstreeks Infrastructure aanroept. De regel is dus niet "Worker mag niet naar de database schrijven", maar **"elke schrijfactie loopt via een Application-command"** — ongeacht vanuit welk Presentation-project. Deze tests draaien in de CI-pipeline (hoofdstuk 18) en zijn een harde build-gate.

---

## 4. Multi-tenancy en toegangsmodel (technische invulling van FO 3.5)

### 4.1 Tenant-scheiding
- Elke tenant-gebonden tabel (`Supplier`, `Invoice`, `Expense`, `Document`, `PurchaseOrder`, `Contract`, `User`, `Branch`, …) heeft een verplichte `OrganizationId`-kolom.
- EF Core `HasQueryFilter(e => e.OrganizationId == _tenantContext.OrganizationId)` wordt op elke tenant-gebonden entiteit toegepast via een gedeelde `ISaveChangesInterceptor` + conventie in `OnModelCreating` (reflectie over alle types die `ITenantEntity` implementeren), zodat dit niet per entiteit handmatig hoeft te gebeuren en niet vergeten kan worden.
- `ITenantContext` is een *scoped* service die bij het begin van elk HTTP-request (Web) of elke Function-invocation (Worker) gevuld wordt — bij Web vanuit de authenticatieclaims, bij Worker vanuit het `OrganizationId` dat in het Service Bus-bericht/`Contracts`-DTO zit.

### 4.2 Branch-gebaseerde zichtbaarheid
Bovenop de tenant-filter geldt de fijnmazigere regel uit FO 3.5 (zichtbaar bij toegang tot **minstens één** gekoppelde vestiging):
- `ICurrentUserContext` levert naast `OrganizationId` ook `IReadOnlyList<Guid> AccessibleBranchIds` (uit een `branch_ids`-claim, of — bij veel vestigingen — opgehaald via een gecachte query zodat het cookie/token niet onbeperkt groeit).
- Voor entiteiten met een **enkelvoudige** branch-relatie (`Invoice`, `Expense`) wordt een tweede global query filter toegepast: `e => accessibleBranchIds.Contains(e.BranchId)`.
- Voor entiteiten met een **meervoudige** branch-relatie (`Supplier` ↔ `Branch`, n-op-n) wordt gefilterd op `e => e.Branches.Any(b => accessibleBranchIds.Contains(b.BranchId))`.
- Bij het **inrichten van een approval flow** (FO 6.4/9.1) wordt bij het opslaan gevalideerd (Application-laag, `ApprovalFlowSettingValidator`) of elke toegewezen `Approver` toegang heeft tot de branch van de flow; zo niet, dan geeft het command een validatiefout/waarschuwing terug die de UI toont.

### 4.3 Rollen en autorisatie
- Rollen (zie vertaaltabel 1.2) zijn **combineerbaar** (FO 3.4) en worden gemodelleerd als claims van het type `role` (meerdere waarden toegestaan), per branch eventueel met een aparte claim-structuur (`role:Approver:branch:{id}`) als rollen per vestiging kunnen verschillen.
- Autorisatie op endpoint-/command-niveau via ASP.NET Core **policy-based authorization**: elke MediatR-command/query implementeert `IRequireRole` met de vereiste rol(len), afgehandeld door een `AuthorizationBehavior` in de MediatR-pipeline (zie 6.3).

---

## 5. Domeinmodel (technische uitwerking)

### 5.1 Aggregates en entities
| Aggregate root | Bevat | Belangrijkste invarianten |
|---|---|---|
| `Organization` | Branches (referentie), SubscriptionPlan | — |
| `Branch` | `AccountingConnection`, `ApprovalFlowSetting`(s) | AccountingConnection en ApprovalFlowSetting horen bij precies één branch |
| `User` | Roles, BranchAccess | — |
| `Supplier` | BranchLinks, Status (`Draft`/`Active`) | Invoices van een `Draft`-supplier mogen niet voorbij status `AwaitingSupplierApproval` |
| `Invoice` | `InvoiceLine[]`, `ApprovalStep[]` | Som van regelbedragen moet overeenkomen met het totaalbedrag (met afrondingsmarge); statusovergangen volgen een vaste state machine (zie 5.3) |
| `ApprovalFlowSetting` | Levels, Supplier-uitzonderingen | Elke toegewezen `Approver` moet toegang hebben tot de branch (zie 4.2) |
| `Expense` | Regel (bonnetje of `Mileage`) | Netto-vergoeding, geen btw-verwerking |
| `Document` | Metadata, koppelingen | — |
| `PurchaseOrder` | `PurchaseOrderLine[]`, ontvangstregistratie | Drie-weg-matching leest dit + gekoppelde `Invoice` |
| `Contract` | — | Signalering gebaseerd op opzegtermijn |

### 5.2 Value objects
`Money` (bedrag + valuta, met optelling/validatie), `Iban`, `ChamberOfCommerceNumber`, `VatNumber`, `EmailAddress`, `Mileage`. Value objects zijn immutable en valideren zichzelf in de constructor (bijv. IBAN-checksum), zodat een ongeldige waarde nooit als geldig domeinobject kan bestaan.

### 5.3 Invoice-statusmachine
```
New -> (AwaitingSupplierApproval) -> Coding -> PendingApproval
     -> Approved -> Processed -> Archived
                  \-> Rejected -> (terug naar Coding)
```
Bewaakt door de `Invoice`-aggregate zelf (methodes zoals `Approve()`, `Reject(reason)`, `SubmitForApproval()`) die een domain-fout geven bij een niet-toegestane overgang. Elke overgang publiceert een **domain event** (`InvoiceApprovedEvent`, `InvoiceRejectedEvent`, …) dat na het committen van de transactie (via de outbox, zie 3.3) als integratiebericht op Service Bus verschijnt indien relevant (bijv. `InvoiceApprovedEvent` triggert de export, zie hoofdstuk 12) — ongeacht of de transactie door de Web App of de Worker werd uitgevoerd.

### 5.4 Invoice lines en codering (FO 6.3)
`InvoiceLine` is een child-entity van `Invoice` met eigen `GeneralLedgerAccountId`, `CostCenterId`, `VatCode` en `Amount`. Validatie "som van regels == totaalbedrag" zit als invariant op de `Invoice`-aggregate (`Invoice.ValidateLinesReconcile()`), aangeroepen vanuit zowel het automatische OCR-verwerkingspad als het handmatige coderen-command.

---

## 6. Application-laag (CQRS)

### 6.1 Commands en queries
Elke use case uit het functioneel ontwerp is een MediatR **command** (schrijvend, bijv. `ApproveInvoiceCommand`, `SubmitExpenseCommand`, `ActivateSupplierCommand`, `ProcessOcrResultCommand`) of **query** (lezend, bijv. `GetInvoicesOverviewQuery`, `GetPendingApprovalsQuery`). Commands retourneren een `Result<T>` (geen exceptions voor verwachte fouten zoals validatie) zodat de UI-laag nette foutmeldingen kan tonen zonder try/catch overal.

### 6.2 Validatie
FluentValidation-validators per command, uitgevoerd door een `ValidationBehavior` in de MediatR-pipeline vóórdat de handler draait. Voorbeeld: `ApproveInvoiceCommandValidator` controleert dat de aanroepende gebruiker daadwerkelijk de aangewezen `Approver` is voor deze invoice/branch (dubbelcheck naast de autorisatielaag, want dit is domeinspecifiek en niet puur een rolcontrole).

### 6.3 Pipeline behaviors (volgorde)
1. **LoggingBehavior** — structured logging van elk command/query (Serilog, gecorreleerd met Application Insights operation id), inclusief welk Presentation-project (Web/Worker) de aanroeper was.
2. **AuthorizationBehavior** — rolcontrole (4.3). Voor Worker-aanroepen (geen ingelogde gebruiker, maar een systeemtrigger) geldt een `SystemPrincipal` met expliciet toegekende systeemrechten, zodat dezelfde behavior-pijplijn ook daar consistent werkt.
3. **ValidationBehavior** — FluentValidation.
4. **TransactionBehavior** — opent een EF Core-transactie rond de handler + flush van de outbox in dezelfde transactie.
5. **PerformanceBehavior** — logt trage handlers (>500ms) als waarschuwing.

### 6.4 Interfaces richting Infrastructure
Application definieert (en Infrastructure implementeert): `IAccountingConnector` (+ `IAccountingConnectorFactory`), `IOcrService`, `IDocumentStorageService`, `IEmailSender`, `IOtpService`, `IUsageMeter` (voor de gebruiksgebaseerde abonnementsvorm, FO 12), `IDateTimeProvider` (`DateTime.UtcNow` injecteerbaar maken t.b.v. testbaarheid).

---

## 7. Blazor Web App

### 7.1 Render mode-strategie
Uitgangspunt: **Static SSR als default**, **Interactive Server** gericht per component ingezet. Concreet:

| Scherm(groep) | Render mode | Reden |
|---|---|---|
| InvoicesOverview, SuppliersOverview, ExpensesOverview, DocumentArchive, ContractsOverview, BudgetOverview (lijsten/formulieren) | Static SSR (met formulier-posts) | Geen continue interactie nodig; snel, weinig serverbelasting |
| InvoiceProcessing (regels toevoegen/verwijderen, live totaalcontrole) | Interactive Server | Dynamisch regels toevoegen zonder page-reload is met Static SSR onhandig |
| Approvals (bulk-acties, live badge-updates) | Interactive Server | Realtime gevoel gewenst, meerdere approvers kunnen gelijktijdig werken |
| Dashboard | Interactive Server | Kaarten kunnen live bijwerken (bijv. teller openstaande approvals) zonder herladen |
| ApprovalFlowSettings (levels samenstellen, live validatie branch-toegang) | Interactive Server | Directe feedback nodig bij het samenstellen van een flow |
| Login (OTP) | Static SSR | Eenvoudige twee-staps formulierflow, geen continue state nodig |

Render mode wordt per component ingesteld (`@rendermode InteractiveServer` op component-niveau, niet globaal op app-niveau).

### 7.2 Projectstructuur binnen MVSoftware.Flexibill.Web
```
MVSoftware.Flexibill.Web/
  Components/
    Pages/
      Invoices/
      Suppliers/
      Approvals/
      Administration/
      Expenses/
      Documents/
      Purchasing/
      Dashboard/
    Shared/                (layout, navigatie, rolafhankelijke menu-items)
  Endpoints/               (Minimal API's, zie hoofdstuk 8)
  Auth/                    (OTP-flow, cookie-authenticatieconfiguratie, JWT-issuer voor de hybride app)
  Program.cs
```

### 7.3 State en autorisatie in de UI
- `AuthenticationStateProvider` leest de cookie-principal (Static SSR) resp. de circuit-principal (Interactive Server) — beide gevuld door dezelfde onderliggende ASP.NET Core-authenticatie, dus geen dubbele implementatie.
- Rolafhankelijke menu-items en dashboardkaarten (FO hoofdstuk 20) worden server-side bepaald op basis van de claims — er wordt nooit clientside "verstopt" wat de gebruiker niet mag zien; niet-geautoriseerde routes geven een 403 op serverniveau.

---

## 8. Minimal API's

### 8.1 Doel en scope
De Minimal API's onder `/api` in hetzelfde App Service-proces bedienen:
- De **hybride mobiele app** (declaratie indienen met foto, FO 7.4; zie hoofdstuk 21.5) — dit is de client die geen Blazor-circuit heeft en dus een "kale" HTTP/JSON-API nodig heeft.
- Toekomstige **integraties** (bijv. een eventueel leveranciersportaal, of webhook-ontvangst vanuit boekhoudpakketten voor betaalstatus-updates, indien het pakket dat ondersteunt in plaats van polling).

### 8.2 Endpoint-indeling (voorbeelden)
```
POST   /api/auth/otp/request
POST   /api/auth/otp/validate         -> JWT access + refresh token
POST   /api/expenses                  (multipart/form-data met foto)
GET    /api/expenses/mine
GET    /api/expenses/{id}
POST   /api/expenses/mileage
GET    /api/branches/mine             (voor branch-keuze in de app)
```
Elke endpoint-groep staat in een eigen `MapXyzEndpoints`-extensiemethode (bijv. `MapExpenseEndpoints`), aangeroepen vanuit `Program.cs`. Endpoints roepen uitsluitend MediatR-commands/queries aan; er zit geen logica in de endpoint-laag zelf, hooguit request/response-mapping.

### 8.3 Authenticatie voor de API
- **JWT Bearer tokens**, uitgegeven na een geslaagde OTP-validatie (dezelfde `IOtpService` als de Blazor-inlogflow, zie 9.1) — een access token (kort, bijv. 15 min) + refresh token (langer, bijv. 30 dagen, met rotatie).
- Reden om hier geen cookie-auth te gebruiken (zoals bij Blazor): een hybride app kan geen browser-cookie delen; JWT is de standaardaanpak voor native/hybride clients.

### 8.4 Validatie en documentatie
FluentValidation ook hier via dezelfde MediatR-pipeline (geen dubbele validatielaag). OpenAPI/Swagger (`Microsoft.AspNetCore.OpenApi`) voor documentatie, alleen zichtbaar in niet-productieomgevingen.

---

## 9. Authenticatie (OTP) en autorisatie — technische flow

### 9.1 OTP-flow (FO 4.2)
1. Gebruiker vult e-mailadres in -> `RequestOtpCommand`.
2. `IOtpService` (Infrastructure) genereert een 6-cijferige code, slaat een **hash** van de code op (nooit de code zelf) samen met `UserId`/e-mailadres, timestamp en pogingenteller, geldig 10 minuten.
3. Verzending via **Azure Communication Services (e-mail)**; bij falen wordt dit gelogd en krijgt de gebruiker een generieke foutmelding (geen technische details).
4. Gebruiker voert code in -> `ValidateOtpCommand` vergelijkt de hash, respecteert een maximumaantal pogingen (bijv. 5) en rate limiting per e-mailadres/IP (ASP.NET Core `RateLimiter`-middleware).
5. Bij succes: Blazor -> cookie-authenticatie (`SignInAsync`) met claims (`sub`, `organization_id`, `role[]`, `branch_ids[]`); hybride app -> JWT-paar.
6. Geen wachtwoord, geen "onthoud mij" — elke sessie vereist een nieuwe OTP na verval van de cookie/token (FO 4.2).

### 9.2 Autorisatie
Zie 4.3. Daarnaast: elke pagina/endpoint die module-gebonden is (bijv. Expense-schermen) controleert ook of de betreffende **module actief is** voor de organization (`IModuleAccessService.IsActive("Expenses")`), zodat een gebruiker met de juiste rol maar een gedeactiveerde module toch geen toegang krijgt.

---

## 10. Documentherkenning (OCR) — Azure AI Document Intelligence

### 10.1 Model
Gebruik van het **prebuilt-invoice**-model van Azure AI Document Intelligence als startpunt: dit model herkent standaard al veel gevraagde velden (leverancier, factuurnummer, data, bedragen, IBAN) **en factuurregels** (omschrijving, aantal, eenheidsprijs, regelbedrag) — sluit direct aan op de regelniveau-codering uit FO 6.2/6.3.

### 10.2 Verwerkingsflow
1. Bij aanlevering (e-mail-inbox of upload) wordt het document naar Blob Storage geschreven (`inbox/{organizationId}/{branchId}/{invoiceId}/original.pdf`).
2. De Web App publiceert (na het schrijven naar de database dat er een nieuwe `Invoice` in status `Coding` klaarstaat) een `InvoiceOcrRequested`-bericht op Service Bus.
3. **Function App** consumeert het bericht, roept Document Intelligence aan, en krijgt gestructureerde data terug inclusief een **confidence score per veld en per regel**.
4. De Function App voert **zelf** — via dezelfde Application-laag als de Web App — het command `ProcessOcrResultCommand` uit:
   - Voor elk veld/elke regel met confidence boven de drempelwaarde (instelbaar, startwaarde bijv. 0,7): automatisch invullen.
   - Voor velden/regels **onder** de drempel: de betreffende `InvoiceLine`(s) krijgen status "handmatig coderen" (FO 6.3).
   - Matching aan `Supplier` (op KVK/IBAN/naam) gebeurt in dit command, met het aanmaken van een `Draft`-supplier als fallback (UC-C2).

### 10.3 Waarom zowel Web App als Function App naar de database schrijven
In een eerdere versie van dit ontwerp stond dat de Function App nooit rechtstreeks zou schrijven en een resultaatbericht terug zou sturen naar de Web App om te verwerken. **Dat is aangepast.** De Application-laag ís de businesslogicalaag: zij verzorgt validatie, invarianten en de daadwerkelijke actie richting de database. Welke "voorkant" een command aanroept — een HTTP-request in de Web App, of een Service Bus-trigger in de Function App — is voor die laag niet relevant. Zolang **elke schrijfactie via een Application-command loopt** (met dezelfde validatie-, autorisatie- en transactiepijplijn, zie 6.3), is het functioneel en architecturaal gelijkwaardig of dat command wordt aangeroepen vanuit Web of vanuit Worker. Dit voorkomt bovendien een onnodige extra bericht-rondtrip (Function → Web → weer terug) en houdt de Service Bus-topologie (hoofdstuk 12) eenvoudiger.

De enige asymmetrie die overblijft: Worker-aanroepen hebben geen ingelogde gebruiker, maar een `SystemPrincipal` met expliciet toegekende, beperkte rechten (zie 6.3 punt 2) — dit is een autorisatie-detail, geen architecturale beperking op wie mag schrijven.

---

## 11. Documentopslag (Blob Storage) en Document Archive-module

### 11.1 Opslagstructuur
Eén Storage Account, met containers gescheiden per documentsoort voor eenvoudiger lifecycle-beleid:
```
invoices/{organizationId}/{branchId}/{invoiceId}/original.<ext>
expenses/{organizationId}/{branchId}/{expenseId}/receipt.<ext>
documents/{organizationId}/{branchId}/{documentId}/{fileName}
```
Elke blob krijgt metadata (`documentType`, `linkedEntityId`, `uploadedAt`) t.b.v. retentiebeheer en indexering.

### 11.2 Retentiebeleid (FO 8.3)
- **Zonder module Document Archive**: een dagelijkse timer-triggered function (`DocumentRetentionFunction`) controleert invoices/expenses waarvan de synchronisatie naar het boekhoudpakket meer dan 3 maanden geleden is gelukt, en verwijdert de bijbehorende blob (niet het boekingsrecord in het boekhoudpakket zelf — dat blijft daar bestaan, zie FO 8.3).
- **Met module actief**: Blob Storage **lifecycle management policy** (ingebouwde Azure-functionaliteit) verplaatst/bewaart op basis van de ingestelde termijn (standaard 7 jaar); de "harde" verwijdering na afloop gebeurt ook via de timer-function, zodat businessregels (zoals de koppeling aan een nog actief contract) meegenomen kunnen worden.

### 11.3 Volledige-tekst zoeken (UC-D1) — v1
Voor v1 wordt **Azure SQL Database Full-Text Search** gebruikt (ingebouwd, geen extra Azure-resource/kosten) op de reeds via Document Intelligence geëxtraheerde tekst. Dit is voor de verwachte documentaantallen van een MKB-klant naar verwachting toereikend. **Azure AI Search** is als nice-to-have genoteerd voor een latere versie, mocht zoeksnelheid/relevantie bij grotere klanten tekortschieten (zie hoofdstuk 21.7) — de architectuur laat dit toe als latere vervanging zonder impact op de Application-laag (achter de `IDocumentSearchService`-interface).

---

## 12. Achtergrondverwerking — Azure Functions en Service Bus

### 12.1 Service Bus-topologie
Dankzij het uitgangspunt uit hoofdstuk 10.3 (beide hosts schrijven via Application-commands) is de topologie eenvoudiger dan in de vorige versie: er is geen apart "resultaat terug naar Web"-bericht meer nodig.

| Naam | Type | Producent | Consument | Doel |
|---|---|---|---|---|
| `invoice-ocr-requested` | Queue | Web App (na upload) | Function App | OCR laten uitvoeren + direct verwerken via `ProcessOcrResultCommand` |
| `invoice-approved` | Topic | Web App of Function App (domain event, ongeacht wie de approval verwerkte) | Function App (export-subscriber) | Trigger `ProcessInvoiceExportCommand` |
| `expense-approved` | Topic | Web App of Function App | Function App (export-subscriber) | Trigger `ProcessExpenseExportCommand` |
| `notifications` | Topic | Web App / Function App (domain events) | Function App (e-mail-subscriber) | Alle e-mailnotificaties (FO 4.4) |

Mislukte verwerkingen komen — via MassTransit — automatisch in de ingebouwde `_error`-queue per consumer terecht (geen los gemodelleerde "export-mislukt"-queue meer nodig); zie 12.3.

### 12.2 Timer-triggered functions
- `DocumentRetentionFunction` (dagelijks) — zie 11.2.
- `ContractExpiryAlertFunction` (dagelijks) — FO 9.3, signalering vóór opzegtermijn.
- `ApprovalEscalationFunction` (elk uur) — FO 6.4, herinnering/escalatie bij niet-gereageerde approvals.
- `BudgetRecalculationFunction` (dagelijks) — vernieuwt/cachet het resterende budget per kostenplaats/periode t.b.v. het dashboard.

Alle timer-functions roepen — net als de Service Bus-consumers — Application-commands aan; ook hier geen rechtstreekse EF Core-writes buiten de Application-laag om.

### 12.3 Betrouwbaarheid
- **Idempotentie**: elk bericht heeft een uniek `MessageId`; consumers slaan verwerkte message-id's kort op (bijv. in een `ProcessedMessage`-tabel) om dubbele verwerking bij at-least-once delivery te voorkomen.
- **Dead-lettering**: MassTransit's ingebouwde `_error`-queues per consumer, met een timer-function die deze periodiek rapporteert aan Administrators (aansluitend op UC-F9, "Mislukte export herstellen").
- **Retry-policy**: exponential backoff via MassTransit, met een maximumaantal pogingen voordat een bericht in de `_error`-queue belandt.

---

## 13. Boekhoudkoppelingen (Accounting Connectors)

### 13.1 Adapter-patroon
Alle boekhoudpakketten uit het functioneel ontwerp zijn **losse, volwaardige connectors** met eigen authenticatie en API-eigenaardigheden — inclusief e-Boekhouden, Snelstart en Yuki afzonderlijk (deze hebben elk een eigen API, ondanks dat ze in het FO als één regel stonden). Dat komt in totaal op zeven pakketten. Eén gemeenschappelijke interface trekt dit gelijk voor de rest van de applicatie:

```csharp
public interface IAccountingConnector
{
    string PackageName { get; }
    Task<AuthenticationResult> AuthenticateAsync(BranchId branchId, CancellationToken ct);
    Task SyncMasterDataAsync(BranchId branchId, CancellationToken ct);
    Task<ExportResult> ProcessInvoiceAsync(InvoiceExportModel model, CancellationToken ct);
    Task<ExportResult> ProcessExpenseAsync(ExpenseExportModel model, CancellationToken ct);
    Task<PaymentStatusResult?> GetPaymentStatusAsync(string externalBookingId, CancellationToken ct);
}
```

Zeven implementaties in Infrastructure: `ExactOnlineConnector`, `AfasConnector`, `VismaNetConnector`, `EBoekhoudenConnector`, `SnelstartConnector`, `YukiConnector`, `EAccountingConnector`. Een `IAccountingConnectorFactory` selecteert de juiste implementatie op basis van de instelling per branch (FO 3.2/6.6).

### 13.2 Credentials en OAuth
OAuth2-tokens (access + refresh) per branch/connectie, **versleuteld opgeslagen** (kolomencryptie of verwijzing naar een Key Vault-secret per connectie). Een timer-function ververst tokens vóór verval. Voor pakketten met API-key in plaats van OAuth geldt hetzelfde beveiligingsniveau via Key Vault.

### 13.3 Regelniveau-export (FO 6.6)
`InvoiceExportModel` bevat een lijst van regels (grootboekrekening, kostenplaats, btw-code, bedrag) plus een bijlage-referentie (het factuurdocument + het gegenereerde approval-rapport, zie 13.4) — elke connector-implementatie mapt dit naar de boekingsvorm van het betreffende pakket.

### 13.4 Approval-rapport
Een lichte PDF-generatie (bijv. met QuestPDF) die per invoice samenvat wie wanneer heeft goedgekeurd; gegenereerd op het moment van export en als tweede bijlage meegestuurd (FO 6.6).

---

## 14. Notificaties

- **E-mail**: Azure Communication Services, aangestuurd vanuit de Function App die de `notifications`-topic consumeert (ontkoppeld van de request-cyclus van de Web App).
- **In-app**: op Interactive Server-pagina's (dashboard, approvals) rechtstreeks via de bestaande SignalR-circuit van Blazor Server — geen aparte SignalR-hub nodig. Static SSR-pagina's tonen de nieuwste stand bij page-load/refresh.
- Voorkeur per gebruiker (alles / alleen belangrijk / uit, FO 4.4) wordt gerespecteerd door de `notifications`-subscriber vóór verzending.

---

## 15. Beveiliging

- **Managed Identity** voor de Web App en Function App richting Azure SQL, Blob Storage, Service Bus en Key Vault — geen connection strings met secrets in configuratie.
- **Azure Key Vault** voor OAuth-tokens van boekhoudkoppelingen, Document Intelligence-sleutel, Communication Services-sleutel.
- **Encryptie**: TLS voor alles onderweg; encryptie-at-rest is standaard voor Azure SQL/Blob Storage; extra kolomencryptie voor OAuth-tokens (13.2).
- **Rate limiting**: op de OTP-aanvraag- en validatie-endpoints (9.1), zowel in de Web App als de Minimal API.
- **Audit trail** (FO 4.6): een `AuditInterceptor` op `DbContext.SaveChangesAsync` die wijzigingen aan gemarkeerde entities (via `IAuditable`) wegschrijft naar een append-only `AuditLog`-tabel (wie, wat, wanneer, oude/nieuwe waarde, en of het via Web of Worker liep), niet aanpasbaar via de normale applicatiepaden.
- **AVG/GDPR**: alle Azure-resources in **West-Europa** regio's. Een expliciet inzage-/verwijderproces is **buiten scope voor v1** (zie hoofdstuk 21.6).

---

## 16. Observability

- **Application Insights** in Web App én Function App, met een gedeelde `Operation Id`/correlatie zodat een verzoek dat via Service Bus van Web naar Worker gaat end-to-end te traceren is.
- **Serilog** als logging-framework, sink naar Application Insights (en Console in lokale ontwikkeling).
- **Health checks** (`/health`) op beide App Services: connectie naar SQL, Service Bus, Blob Storage.
- **Dashboards/alerts**: alert bij een groeiende `_error`-queue, bij mislukte exports boven een drempel, en bij OCR-verwerkingstijd boven de NFE-norm (FO hoofdstuk 13: binnen ~1 minuut).

---

## 17. Testen

| Laag | Aanpak |
|---|---|
| Domain | Unit tests (xUnit + FluentAssertions) op aggregates/value objects, met name de state machine (5.3) en regel-validatie (5.4) |
| Application | Unit tests op command/query handlers met gemockte Infrastructure-interfaces (NSubstitute); losse tests per pipeline behavior, inclusief tests die bevestigen dat een command hetzelfde resultaat geeft ongeacht of de aanroeper Web of Worker "simuleert" |
| Infrastructure | Integratietests met **Testcontainers** (echte SQL Server-container) voor EF Core-mappings en query filters (met name de multi-tenancy-/branch-filters uit hoofdstuk 4) |
| Architecture | NetArchTest-regels (3.4) |
| Web (end-to-end) | Playwright-tests voor de belangrijkste flows: inloggen (OTP), invoice coderen + approven, expense indienen |
| Berichten | MassTransit's in-memory test harness voor de Service Bus-consumers |

---

## 18. CI/CD en omgevingen

- **Pipeline**: **Azure DevOps Pipelines** (YAML-pipelines), met aparte pipelines voor Web, Worker en de gedeelde libraries (Domain/Application/Infrastructure als NuGet-achtige build-artefacten binnen dezelfde repo/solution).
- **Infrastructure as Code**: Bicep-templates per omgeving (dev/test/acceptatie/productie), met aparte resource groups.
- **Pipeline-stappen**: build -> unit- en architecture-tests -> integratietests (Testcontainers) -> security/dependency scan -> deploy naar dev (automatisch) -> handmatige goedkeuring voor acceptatie/productie -> deploy via deployment slots (swap na health check).
- **Database-migraties**: EF Core-migraties, uitgevoerd als aparte pipeline-stap vóór de slot-swap (nooit impliciet bij opstarten van de app in productie).

---

## 19. Niet-functionele eisen — technische vertaling

| FO-eis (hoofdstuk 13) | Technische invulling |
|---|---|
| Multi-tenancy, volledige scheiding van data | Hoofdstuk 4 (global query filters) + geen gedeelde caches tussen tenants |
| Beschikbaarheid ~99,5% | App Service + Function App elk met minimaal 2 instanties. Azure SQL Database in **één regio met point-in-time restore** (geen failover-groep, bewust gekozen i.v.m. kosten, zie hoofdstuk 21.3) |
| Auditability | Hoofdstuk 15 (AuditLog) |
| Gebruiksregistratie (FO 12) | `IUsageMeter` telt verwerkte invoices/expenses per organization per periode, weggeschreven naar een aparte `UsageRecord`-tabel; los proces koppelt dit aan facturatie (buiten scope van dit ontwerp) |
| Performance OCR (~1 minuut) | Asynchrone verwerking via Service Bus (hoofdstuk 10) i.p.v. de gebruiker te laten wachten tijdens upload |
| Mobiel (declaraties) | Minimal API + JWT (hoofdstuk 8/9), geconsumeerd door een hybride app (zie hoofdstuk 21.5) |

---

## 20. Traceerbaarheid: functionele modules naar technische bouwstenen

| Functionele module (FO) | Aggregates | Belangrijkste Service Bus-berichten | Belangrijkste Azure-diensten |
|---|---|---|---|
| CRM Leveranciers | `Supplier` | — | Azure SQL |
| Factuurverwerking (incl. fiattering, boekhoudkoppeling) | `Invoice`, `ApprovalFlowSetting` | `invoice-ocr-requested`, `invoice-approved` | Document Intelligence, Service Bus, accounting-API's |
| Beheer/Inloggen | `User`, `Organization`, `Branch` | — | Communication Services (OTP) |
| Documentarchief | `Document` | — | Blob Storage, SQL Full-Text Search |
| Declaratieverwerking | `Expense` | `expense-approved` | Blob Storage (bonnetjes), Minimal API (hybride app) |
| Inkoopmanagement | `PurchaseOrder`, `Contract` | — | Azure SQL, timer-functions (signalering) |

---

## 21. Aannames, beslissingen en openstaande punten

Beantwoorde punten uit de vorige versie:

1. **.NET-versie**: **.NET 10 (LTS, uitgebracht november 2025)** — dit is inmiddels de actuele LTS-versie en dus de basis van dit ontwerp. Blazor Web App-functionaliteit (render modes, Static SSR/Interactive Server) is in .NET 10 ongewijzigd bruikbaar t.o.v. .NET 8.
2. **CI/CD-platform**: **Azure DevOps Pipelines** (hoofdstuk 18).
3. **Beschikbaarheid/SLA**: **één regio met point-in-time restore**, geen failover-groep (hoofdstuk 19).
4. **Boekhoudkoppelingen**: **alle pakketten zijn losse connectors** (ook e-Boekhouden/Snelstart/Yuki afzonderlijk), gelijkgetrokken via `IAccountingConnector` (hoofdstuk 13).
5. **Mobiele app**: wordt een **hybride app**; de Minimal API's (hoofdstuk 8) zijn hiervoor nodig. Het exacte hybride framework (bijv. .NET MAUI Blazor Hybrid, of een Capacitor/Ionic-achtige aanpak) is nog niet bepaald — dit heeft geen impact op dit technisch ontwerp, alleen op een apart (mobiel) technisch ontwerp.
6. **AVG-inzage/verwijderverzoeken**: **buiten scope voor v1**.
7. **Zoeken in het documentarchief**: **Azure SQL Full-Text Search voor v1**; Azure AI Search is een nice-to-have voor een latere versie (hoofdstuk 11.3).

Nog openstaand:

- **Hybride app-framework**: welke technologie (MAUI Blazor Hybrid, Capacitor, anders) — te beslissen zodra de mobiele kant concreet wordt opgepakt; geen blokkade voor dit document.

---

*Dit document is een concept en nog niet omgezet naar Word. Graag verder controleren; na akkoord kan dit — net als het functioneel ontwerp — als opgemaakt .docx-bestand opgeleverd worden.*
