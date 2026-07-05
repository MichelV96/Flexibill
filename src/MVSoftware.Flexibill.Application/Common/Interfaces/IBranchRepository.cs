using MVSoftware.Flexibill.Domain.Branches;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid branchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Branch>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken);
    Task AddAsync(Branch branch, CancellationToken cancellationToken);
    Task UpdateAsync(Branch branch, CancellationToken cancellationToken);
}
