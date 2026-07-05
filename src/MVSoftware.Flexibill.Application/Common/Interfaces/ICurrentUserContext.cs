using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>
/// De ingelogde gebruiker, opgebouwd uit de claims die tijdens het inloggen zijn gezet
/// (Technisch Ontwerp, hoofdstuk 4.1-4.3, 9.1). Scoped per request/Function-invocation.
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid OrganizationId { get; }
    string DisplayName { get; }
    IReadOnlyCollection<UserRole> Roles { get; }
    IReadOnlyCollection<Guid> BranchIds { get; }

    bool HasRole(UserRole role);
}
