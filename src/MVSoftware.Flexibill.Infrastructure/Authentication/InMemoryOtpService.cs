using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure.Authentication;

/// <summary>
/// TIJDELIJKE in-memory implementatie van IOtpService (Technisch Ontwerp, hoofdstuk 9.1).
///
/// TODO (volgende stap): vervang door een implementatie die de gehashte code + expiry
/// + pogingenteller in Azure SQL Database opslaat (via de EF Core DbContext), zodat dit
/// werkt over meerdere instanties van de Web App heen. Deze in-memory versie is alleen
/// bruikbaar voor lokale ontwikkeling met één proces.
/// </summary>
public sealed class InMemoryOtpService(ILogger<InMemoryOtpService> logger) : IOtpService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private const int MaxAttempts = 5;

    private readonly ConcurrentDictionary<string, OtpEntry> _codesByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<string> GenerateCodeAsync(string email, CancellationToken cancellationToken)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var entry = new OtpEntry(Hash(code), DateTimeOffset.UtcNow.Add(CodeLifetime), Attempts: 0);
        _codesByEmail[email] = entry;

        logger.LogInformation("Generated OTP for {Email} (valid until {ExpiresAtUtc})", email, entry.ExpiresAtUtc);
        return Task.FromResult(code);
    }

    public Task<bool> ValidateCodeAsync(string email, string code, CancellationToken cancellationToken)
    {
        if (!_codesByEmail.TryGetValue(email, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAtUtc < DateTimeOffset.UtcNow || entry.Attempts >= MaxAttempts)
        {
            _codesByEmail.TryRemove(email, out _);
            return Task.FromResult(false);
        }

        if (entry.Hash != Hash(code))
        {
            _codesByEmail[email] = entry with { Attempts = entry.Attempts + 1 };
            return Task.FromResult(false);
        }

        // Eenmalig bruikbaar (FO 4.2): na een geslaagde validatie is de code direct ongeldig.
        _codesByEmail.TryRemove(email, out _);
        return Task.FromResult(true);
    }

    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));

    private sealed record OtpEntry(string Hash, DateTimeOffset ExpiresAtUtc, int Attempts);
}
