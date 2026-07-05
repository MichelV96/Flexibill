using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Infrastructure.Persistence;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository(FlexibillDbContext dbContext) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(Guid branchId, CancellationToken cancellationToken) =>
        dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Branches.Where(b => b.OrganizationId == organizationId).ToListAsync(cancellationToken);

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken)
    {
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
