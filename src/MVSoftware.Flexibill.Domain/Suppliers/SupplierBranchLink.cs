using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Suppliers;

/// <summary>
/// Koppelt een leverancier aan een vestiging die ermee mag werken (FO 5.1). Bepaalt,
/// samen met de branch-toegang van de gebruiker, de zichtbaarheid (Technisch Ontwerp, 4.2).
/// </summary>
public sealed class SupplierBranchLink : Entity
{
    public Guid BranchId { get; private set; }

    private SupplierBranchLink() { }

    internal static SupplierBranchLink Create(Guid branchId) => new() { BranchId = branchId };
}
