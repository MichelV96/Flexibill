using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Infrastructure.Authentication;

/// <summary>
/// Leest de claims die bij het inloggen zijn gezet (Technisch Ontwerp, hoofdstuk 9.1)
/// uit de huidige HttpContext. Scoped per request - werkt dus alleen in de Web App;
/// de Worker bouwt zijn eigen (systeem-)principal op vanuit het Service Bus-bericht
/// (hoofdstuk 6.3, punt 2), niet vanuit deze klasse.
/// </summary>
public sealed class HttpContextCurrentUserContext : ICurrentUserContext
{
    private readonly ClaimsPrincipal _principal;

    public HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HttpContext is available to resolve the current user from.");
    }

    public Guid UserId => Guid.Parse(_principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current user has no NameIdentifier claim."));

    public Guid OrganizationId => Guid.Parse(_principal.FindFirstValue("organization_id")
        ?? throw new InvalidOperationException("The current user has no organization_id claim."));

    public string DisplayName => _principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public IReadOnlyCollection<UserRole> Roles => _principal.FindAll(ClaimTypes.Role)
        .Select(c => Enum.Parse<UserRole>(c.Value))
        .ToList();

    public IReadOnlyCollection<Guid> BranchIds => _principal.FindAll("branch_id")
        .Select(c => Guid.Parse(c.Value))
        .ToList();

    public bool HasRole(UserRole role) => Roles.Contains(role);
}
