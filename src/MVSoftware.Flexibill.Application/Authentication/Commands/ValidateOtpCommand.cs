using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Authentication.Commands;

/// <summary>UC-L1, stap 4-5: valideert de ingevoerde code en start de sessie.</summary>
public sealed record ValidateOtpCommand(string Email, string Code) : IRequest<Result<AuthenticatedUser>>;

public sealed class ValidateOtpCommandValidator : AbstractValidator<ValidateOtpCommand>
{
    public ValidateOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}

public sealed class ValidateOtpCommandHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ValidateOtpCommand, Result<AuthenticatedUser>>
{
    public async Task<Result<AuthenticatedUser>> Handle(ValidateOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await otpService.ValidateCodeAsync(request.Email, request.Code, cancellationToken);
        if (!isValid)
        {
            return Result<AuthenticatedUser>.Failure("De code is onjuist of verlopen. Vraag een nieuwe code aan.");
        }

        var user = await userRepository.GetByEmailAcrossOrganizationsAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<AuthenticatedUser>.Failure("De code is onjuist of verlopen. Vraag een nieuwe code aan.");
        }

        user.RecordLogin(dateTimeProvider.UtcNow);

        var authenticatedUser = new AuthenticatedUser(
            user.Id, user.OrganizationId, user.Email.Value, user.DisplayName, user.Roles, user.BranchIds);

        return Result<AuthenticatedUser>.Success(authenticatedUser);
    }
}
