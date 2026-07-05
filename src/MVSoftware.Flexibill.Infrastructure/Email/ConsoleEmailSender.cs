using Microsoft.Extensions.Logging;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure.Email;

/// <summary>
/// TIJDELIJKE implementatie van IEmailSender die e-mails logt in plaats van verstuurt.
///
/// TODO (volgende stap): vervang door een implementatie op basis van Azure Communication
/// Services Email (Technisch Ontwerp, hoofdstuk 9.1, 14), met de sleutel uit Key Vault.
/// </summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV E-MAIL] Aan: {To} | Onderwerp: {Subject} | Inhoud: {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
