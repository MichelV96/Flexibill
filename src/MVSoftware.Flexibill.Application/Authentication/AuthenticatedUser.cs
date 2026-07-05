using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Authentication;

/// <summary>
/// Alle gegevens die de Web App nodig heeft om na een geslaagde OTP-validatie een
/// cookie-principal (of, voor de hybride app, een JWT) op te bouwen met de claims uit
/// Technisch Ontwerp, hoofdstuk 9.1: sub, organization_id, role[], branch_ids[].
/// </summary>
public sealed record AuthenticatedUser(
    Guid UserId,
    Guid OrganizationId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<UserRole> Roles,
    IReadOnlyCollection<Guid> BranchIds);
