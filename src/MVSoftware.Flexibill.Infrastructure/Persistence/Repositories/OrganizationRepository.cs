using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Organizations;
using MVSoftware.Flexibill.Infrastructure.Persistence;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository(FlexibillDbContext dbContext) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
}
