using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

public interface IUserRepository
{
    /// <summary>
    /// Zoekt een gebruiker op e-mailadres, ORGANISATIE-OVERSCHRIJDEND (negeert de tenant-
    /// queryfilter). E-mail is platformbreed uniek en bepaalt bij het inloggen zelf al bij welke
    /// organisatie iemand hoort - er is dus nog geen "huidige organisatie" om op te filteren.
    /// Gebruik deze methode alleen waar dat precies de bedoeling is (login-flow, uniekheids-
    /// check bij uitnodigen); voor een tenant-gescoopte lookup binnen de huidige organisatie is
    /// deze methode NIET geschikt.
    /// </summary>
    Task<User?> GetByEmailAcrossOrganizationsAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
}
