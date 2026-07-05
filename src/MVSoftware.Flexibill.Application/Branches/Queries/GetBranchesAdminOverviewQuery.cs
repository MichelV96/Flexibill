using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Branches.Queries;

public sealed record BranchAdminSummaryDto(
    Guid Id,
    string Name,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City,
    bool IsAccountingConnected);

/// <summary>Vestigingenbeheerscherm (UC-B2, FO 3.2) - rijker dan GetBranchesOverviewQuery, die
/// bewust minimaal blijft t.b.v. dropdowns elders (Users.razor, ApprovalFlow.razor).</summary>
public sealed record GetBranchesAdminOverviewQuery : IRequest<IReadOnlyList<BranchAdminSummaryDto>>;

public sealed class GetBranchesAdminOverviewQueryHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository) : IRequestHandler<GetBranchesAdminOverviewQuery, IReadOnlyList<BranchAdminSummaryDto>>
{
    public async Task<IReadOnlyList<BranchAdminSummaryDto>> Handle(GetBranchesAdminOverviewQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(currentUser.OrganizationId, cancellationToken);

        return branches
            .Select(branch => new BranchAdminSummaryDto(
                branch.Id,
                branch.Name,
                branch.Address?.Street,
                branch.Address?.HouseNumber,
                branch.Address?.PostalCode,
                branch.Address?.City,
                branch.IsAccountingConnected))
            .OrderBy(b => b.Name)
            .ToList();
    }
}
