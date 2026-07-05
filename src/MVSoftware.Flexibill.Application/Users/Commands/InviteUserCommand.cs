using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Users.Commands;

/// <summary>UC-B1: Beheerder nodigt een gebruiker uit met rol(len) en vestiging(en).</summary>
public sealed record InviteUserCommand(
    string Email,
    string DisplayName,
    IReadOnlyList<UserRole> Roles,
    IReadOnlyList<Guid> BranchIds) : IRequest<Result<Guid>>;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class InviteUserCommandHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository) : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // FO 4.3: "E-mailadres al in gebruik... geen dubbele uitnodiging mogelijk". E-mail is
        // platformbreed uniek (het bepaalt bij het inloggen zelf al bij welke organisatie iemand hoort).
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Failure("Dit e-mailadres is al in gebruik.");
        }

        var user = User.Invite(
            currentUser.OrganizationId,
            EmailAddress.Of(request.Email),
            request.DisplayName,
            request.Roles,
            request.BranchIds);

        await userRepository.AddAsync(user, cancellationToken);

        // TODO: uitnodigingsmail versturen (IEmailSender) zodra er een aparte "welkom"-flow is;
        // voor nu kan de gebruiker direct via de gewone OTP-inlog (FO 4.2) inloggen.
        return Result<Guid>.Success(user.Id);
    }
}
