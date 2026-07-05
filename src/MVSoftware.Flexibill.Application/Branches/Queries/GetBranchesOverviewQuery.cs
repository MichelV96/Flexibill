using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Branches.Queries;

public sealed record BranchOptionDto(Guid Id, string Name);

/// <summary>Simpele lijst t.b.v. dropdowns (vestigingskeuze bij het uitnodigen/bewerken van een gebruiker).</summary>
public sealed record GetBranchesOverviewQuery : IRequest<IReadOnlyList<BranchOptionDto>>;

public sealed class GetBranchesOverviewQueryHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository) : IRequestHandler<GetBranchesOverviewQuery, IReadOnlyList<BranchOptionDto>>
{
    public async Task<IReadOnlyList<BranchOptionDto>> Handle(GetBranchesOverviewQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(currentUser.OrganizationId, cancellationToken);
        return branches.OrderBy(b => b.Name).Select(b => new BranchOptionDto(b.Id, b.Name)).ToList();
    }
}
