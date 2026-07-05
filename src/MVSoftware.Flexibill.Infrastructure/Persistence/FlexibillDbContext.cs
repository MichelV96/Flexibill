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
    /// heeft tot <see cref="currentUserContext"/>).
    /// </summary>
    private void ApplySupplierBranchVisibilityFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>().HasQueryFilter(
            "Branch",
            (Supplier s) => s.BranchLinks.Any(l => currentUserContext.BranchIds.Contains(l.BranchId)));
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

            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var organizationIdAccess = System.Linq.Expressions.Expression.Call(
                    typeof(EF), nameof(EF.Property), [typeof(Guid)], parameter,
                    System.Linq.Expressions.Expression.Constant(nameof(ITenantEntity.OrganizationId)));
                var currentOrganizationId = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(currentUserContext), nameof(ICurrentUserContext.OrganizationId));
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
                var accessibleBranchIds = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(currentUserContext), nameof(ICurrentUserContext.BranchIds));
                var containsCall = System.Linq.Expressions.Expression.Call(
                    typeof(Enumerable), nameof(Enumerable.Contains), [typeof(Guid)], accessibleBranchIds, branchIdAccess);
                var branchFilter = System.Linq.Expressions.Expression.Lambda(containsCall, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter("Branch", branchFilter);
            }
        }
    }
}
