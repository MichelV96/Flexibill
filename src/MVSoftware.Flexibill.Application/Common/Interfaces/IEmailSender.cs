namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>
/// Verstuurt e-mail. Infrastructure-implementatie gebruikt Azure Communication Services
/// (Technisch Ontwerp, hoofdstuk 9.1, 14).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}
