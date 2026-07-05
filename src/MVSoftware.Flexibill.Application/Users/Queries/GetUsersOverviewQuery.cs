using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Users.Queries;

public sealed record UserSummaryDto(
    Guid Id,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<Guid> BranchIds,
    IReadOnlyCollection<string> BranchNames,
    DateTimeOffset? LastLoginAtUtc,
    bool IsActive);

/// <summary>UC-C5-achtig overzicht, maar dan voor gebruikers (FO 4.3, wireframe "Gebruikersbeheer").</summary>
public sealed record GetUsersOverviewQuery : IRequest<IReadOnlyList<UserSummaryDto>>;

public sealed class GetUsersOverviewQueryHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository,
    IBranchRepository branchRepository) : IRequestHandler<GetUsersOverviewQuery, IReadOnlyList<UserSummaryDto>>
{
    public async Task<IReadOnlyList<UserSummaryDto>> Handle(GetUsersOverviewQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(currentUser.OrganizationId, cancellationToken);
        var branches = await branchRepository.GetAllAsync(currentUser.OrganizationId, cancellationToken);
        var branchNamesById = branches.ToDictionary(b => b.Id, b => b.Name);

        return users
            .Select(user => new UserSummaryDto(
                user.Id,
                user.DisplayName,
                user.Email.Value,
                user.Roles.Select(r => r.ToString()).ToList(),
                user.BranchIds.ToList(),
                user.BranchIds.Select(id => branchNamesById.GetValueOrDefault(id, "(onbekende vestiging)")).ToList(),
                user.LastLoginAtUtc,
                user.IsActive))
            .OrderBy(u => u.DisplayName)
            .ToList();
    }
}
