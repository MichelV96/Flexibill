using MVSoftware.Flexibill.Domain.Suppliers;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

public interface ISupplierRepository
{
    Task<int> CountDraftAsync(Guid organizationId, CancellationToken cancellationToken);

    /// <summary>UC-C6: actieve leveranciers met ontbrekende verplichte gegevens.</summary>
    Task<int> CountActiveWithMissingDataAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken);

    /// <summary>Leveranciers gekoppeld aan een vestiging (FO 5.1) - t.b.v. de leverancier-keuze bij een fiaterings-uitzondering.</summary>
    Task<IReadOnlyList<Supplier>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken);
}
