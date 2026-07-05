using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Users.Commands;

/// <summary>FO 4.3: rollen en/of vestigingstoegang van een bestaande gebruiker wijzigen.</summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    IReadOnlyList<UserRole> Roles,
    IReadOnlyList<Guid> BranchIds) : IRequest<Result>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class UpdateUserCommandHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository) : IRequestHandler<UpdateUserCommand, Result>
{
    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || user.OrganizationId != currentUser.OrganizationId)
        {
            return Result.Failure("De gebruiker is niet gevonden.");
        }

        user.UpdateRolesAndBranches(request.Roles, request.BranchIds);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
