using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Authentication.Commands;

/// <summary>UC-L1, stap 1-3: vraagt een OTP-code aan voor het opgegeven e-mailadres.</summary>
public sealed record RequestOtpCommand(string Email) : IRequest<Result>;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class RequestOtpCommandHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IEmailSender emailSender) : IRequestHandler<RequestOtpCommand, Result>
{
    public async Task<Result> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // FO 4.2 / UC-L1: bij een onbekend e-mailadres geven we bewust dezelfde,
        // generieke uitkomst terug - we lekken niet of een account bestaat.
        var user = await userRepository.GetByEmailAcrossOrganizationsAsync(request.Email, cancellationToken);
        if (user is not null && user.IsActive)
        {
            var code = await otpService.GenerateCodeAsync(request.Email, cancellationToken);
            await emailSender.SendAsync(
                request.Email,
                subject: "Je Flexibill-inlogcode",
                body: $"Je eenmalige inlogcode is: {code}. Deze code is 10 minuten geldig.",
                cancellationToken);
        }

        return Result.Success();
    }
}
