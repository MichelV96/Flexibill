using MVSoftware.Flexibill.Domain.Organizations;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken);
}
