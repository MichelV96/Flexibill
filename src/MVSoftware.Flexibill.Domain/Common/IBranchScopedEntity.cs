namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Marks an entity as belonging to a single branch. Used by the branch-based
/// visibility query filter (Technisch Ontwerp, hoofdstuk 4.2).
/// </summary>
public interface IBranchScopedEntity
{
    Guid BranchId { get; }
}
