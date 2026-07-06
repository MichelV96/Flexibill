using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Invoices;
using MVSoftware.Flexibill.Domain.Organizations;
using MVSoftware.Flexibill.Domain.Suppliers;
using MVSoftware.Flexibill.Domain.Users;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;
using MVSoftware.Flexibill.Infrastructure.Persistence.Outbox;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

/// <summary>
/// De EF Core DbContext (Technisch Ontwerp, hoofdstuk 3.1). Eén gedeelde Azure SQL Database
/// voor alle tenants, met multi-tenancy en branch-zichtbaarheid afgedwongen via global query
/// filters (hoofdstuk 4) - zie <see cref="OnModelCreating"/>.
/// </summary>
public sealed class FlexibillDbContext(
    DbContextOptions<FlexibillDbContext> options,
    ICurrentUserContext currentUserContext) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ApprovalFlowSetting> ApprovalFlowSettings => Set<ApprovalFlowSetting>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// BEWUST een property i.p.v. de constructorparameter <c>currentUserContext</c> rechtstreeks
    /// in de query filters te gebruiken - zie de toelichting bij <see cref="OnModelCreating"/>
    /// voor waarom dat verschil essentieel is.
    /// </summary>
    private ICurrentUserContext CurrentUserContext => currentUserContext;

    /// <summary>
    /// EF Core cachet het gebouwde model (inclusief query filters) PER DBCONTEXT-TYPE, voor de
    /// hele levensduur van het proces - <see cref="OnModelCreating"/> draait dus maar één keer
    /// per proces, niet één keer per request/scope. Filters die met
    /// <c>Expression.Constant(currentUserContext)</c> een SPECIFIEKE <see cref="ICurrentUserContext"/>
    /// -instantie bevriezen (zoals dit bestand tot voor kort deed) bevatten daardoor voor de rest
    /// van het procesleven de allereerste ooit geïnjecteerde instantie - typisch die van de
    /// allereerste, nog niet ingelogde aanroep (bijv. `RequestOtpCommand` op het inlogscherm) -
    /// en geven daarna structureel de verkeerde (of helemaal geen) organisatie/vestigingen terug,
    /// ONGEACHT wie er daadwerkelijk is ingelogd. Dit gaf de intermittente "geen organization_id-
    /// claim"-crash die leek op een Blazor-renderrace, maar in werkelijkheid dit was.
    ///
    /// De fix: <c>Expression.Constant(this)</c> gebruiken (i.p.v. de service rechtstreeks) en
    /// daarna via property-access naar <see cref="CurrentUserContext"/> lopen. EF Core herkent
    /// een `ConstantExpression` die de DbContext-instantie zelf bevat expliciet en vervangt die
    /// bij elke query-uitvoering door de ECHTE, huidige instantie - dit is het door Microsoft
    /// gedocumenteerde patroon voor "instance state" in global query filters, en is (in
    /// tegenstelling tot een willekeurige constante) NIET gevoelig voor modelcaching.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlexibillDbContext).Assembly);

        ApplyMultiTenancyQueryFilters(modelBuilder);
        ApplySupplierBranchVisibilityFilter(modelBuilder);
    }

    /// <summary>
    /// Technisch Ontwerp, hoofdstuk 4.2: Supplier heeft een n-op-n branch-relatie (via
    /// BranchLinks), dus geen <see cref="IBranchScopedEntity"/> zoals Invoice/ApprovalFlowSetting
    /// - dit vergt een eigen filtervorm die niet met de generieke reflectielus valt te
    /// combineren, en staat daarom hier (i.p.v. in SupplierConfiguration, die geen toegang
    /// heeft tot <see cref="CurrentUserContext"/>).
    /// </summary>
    private void ApplySupplierBranchVisibilityFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>().HasQueryFilter(
            "Branch",
            (Supplier s) => s.BranchLinks.Any(l => CurrentUserContext.BranchIds.Contains(l.BranchId)));
    }

    /// <summary>
    /// Technisch Ontwerp, hoofdstuk 4.1/4.2: elke <see cref="ITenantEntity"/> krijgt een
    /// benoemd "Tenant"-filter op OrganizationId; elke <see cref="IBranchScopedEntity"/> krijgt
    /// daarnaast een benoemd "Branch"-filter op BranchId. EF Core combineert meerdere benoemde
    /// filters op dezelfde entiteit automatisch met AND. Via reflectie zodat dit niet per
    /// entiteit handmatig herhaald hoeft te worden en niet vergeten kan worden (zie ook
    /// SupplierConfiguration voor de aparte n-op-n-variant van dit filter).
    /// </summary>
    private void ApplyMultiTenancyQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var dbContextConstant = System.Linq.Expressions.Expression.Constant(this);

            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var organizationIdAccess = System.Linq.Expressions.Expression.Call(
                    typeof(EF), nameof(EF.Property), [typeof(Guid)], parameter,
                    System.Linq.Expressions.Expression.Constant(nameof(ITenantEntity.OrganizationId)));
                var currentUserContextAccess = System.Linq.Expressions.Expression.Property(
                    dbContextConstant, nameof(CurrentUserContext));
                var currentOrganizationId = System.Linq.Expressions.Expression.Property(
                    currentUserContextAccess, nameof(ICurrentUserContext.OrganizationId));
                var tenantFilter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(organizationIdAccess, currentOrganizationId), parameter);

                modelBuilder.Entity(clrType).HasQueryFilter("Tenant", tenantFilter);
            }

            if (typeof(IBranchScopedEntity).IsAssignableFrom(clrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var branchIdAccess = System.Linq.Expressions.Expression.Call(
                    typeof(EF), nameof(EF.Property), [typeof(Guid)], parameter,
                    System.Linq.Expressions.Expression.Constant(nameof(IBranchScopedEntity.BranchId)));
                var currentUserContextAccess = System.Linq.Expressions.Expression.Property(
                    dbContextConstant, nameof(CurrentUserContext));
                var accessibleBranchIds = System.Linq.Expressions.Expression.Property(
                    currentUserContextAccess, nameof(ICurrentUserContext.BranchIds));
                var containsCall = System.Linq.Expressions.Expression.Call(
                    typeof(Enumerable), nameof(Enumerable.Contains), [typeof(Guid)], accessibleBranchIds, branchIdAccess);
                var branchFilter = System.Linq.Expressions.Expression.Lambda(containsCall, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter("Branch", branchFilter);
            }
        }
    }
}
