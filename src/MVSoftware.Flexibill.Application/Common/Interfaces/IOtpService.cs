namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>
/// Genereert, bewaart (gehasht) en valideert eenmalige inlogcodes (Technisch Ontwerp,
/// hoofdstuk 9.1). Het daadwerkelijk versturen van de code gebeurt door <see cref="IEmailSender"/>.
/// </summary>
public interface IOtpService
{
    /// <summary>Genereert een nieuwe code voor <paramref name="email"/> en retourneert deze zodat hij verstuurd kan worden.</summary>
    Task<string> GenerateCodeAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Valideert de ingevoerde code. Retourneert false bij een onjuiste/verlopen code
    /// of als het maximumaantal pogingen is overschreden (FO 4.2).
    /// </summary>
    Task<bool> ValidateCodeAsync(string email, string code, CancellationToken cancellationToken);
}
