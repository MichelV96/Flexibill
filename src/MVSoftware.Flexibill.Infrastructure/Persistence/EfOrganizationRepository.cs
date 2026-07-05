using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Organizations;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

public sealed class EfOrganizationRepository(FlexibillDbContext dbContext) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
}
