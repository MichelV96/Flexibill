namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Marks an entity as belonging to a single organization (tenant).
/// Used by the EF Core global query filter convention in Infrastructure
/// (Technisch Ontwerp, hoofdstuk 4.1) to enforce multi-tenancy automatically.
/// </summary>
public interface ITenantEntity
{
    Guid OrganizationId { get; }
}
