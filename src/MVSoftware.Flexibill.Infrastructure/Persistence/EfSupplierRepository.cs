using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Suppliers;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

public sealed class EfSupplierRepository(FlexibillDbContext dbContext) : ISupplierRepository
{
    public Task<int> CountDraftAsync(Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Suppliers.CountAsync(s => s.OrganizationId == organizationId && s.Status == SupplierStatus.Draft, cancellationToken);

    /// <summary>
    /// UC-C6: <see cref="Supplier.HasMissingRequiredFields"/> is domain-logica die niet naar SQL
    /// vertaald wordt - actieve leveranciers van de organisatie worden gematerialiseerd
    /// (inclusief hun owned Ibans/BranchLinks, die EF Core automatisch meeneemt) en daarna
    /// in-memory geteld, net als de eerdere in-memory repository deed.
    /// </summary>
    public async Task<int> CountActiveWithMissingDataAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var activeSuppliers = await dbContext.Suppliers
            .Where(s => s.OrganizationId == organizationId && s.Status == SupplierStatus.Active)
            .ToListAsync(cancellationToken);

        return activeSuppliers.Count(s => s.HasMissingRequiredFields());
    }

    public Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken cancellationToken) =>
        dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);

    public async Task<IReadOnlyList<Supplier>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken) =>
        await dbContext.Suppliers.Where(s => s.BranchLinks.Any(l => l.BranchId == branchId)).ToListAsync(cancellationToken);
}
