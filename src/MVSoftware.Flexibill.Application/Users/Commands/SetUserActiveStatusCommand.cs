using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Users.Commands;

/// <summary>
/// FO 4.3: "Gebruiker kan gedeactiveerd worden (i.p.v. verwijderd, i.v.m. audit trail/historie)".
/// Eén command voor beide richtingen (deactiveren/heractiveren) omdat het dezelfde,
/// symmetrische operatie is.
/// </summary>
public sealed record SetUserActiveStatusCommand(Guid UserId, bool IsActive) : IRequest<Result>;

public sealed class SetUserActiveStatusCommandValidator : AbstractValidator<SetUserActiveStatusCommand>
{
    public SetUserActiveStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class SetUserActiveStatusCommandHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository) : IRequestHandler<SetUserActiveStatusCommand, Result>
{
    public async Task<Result> Handle(SetUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || user.OrganizationId != currentUser.OrganizationId)
        {
            return Result.Failure("De gebruiker is niet gevonden.");
        }

        if (user.Id == currentUser.UserId && !request.IsActive)
        {
            return Result.Failure("Je kunt jezelf niet deactiveren.");
        }

        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        return Result.Success();
    }
}
