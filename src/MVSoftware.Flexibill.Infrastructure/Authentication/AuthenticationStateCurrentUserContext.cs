using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Infrastructure.Authentication;

/// <summary>
/// Leest de claims die bij het inloggen zijn gezet (Technisch Ontwerp, hoofdstuk 9.1) via
/// <see cref="AuthenticationStateProvider"/> - scoped per request/circuit, werkt dus alleen in
/// de Web App; de Worker bouwt zijn eigen (systeem-)principal op vanuit het Service Bus-bericht
/// (hoofdstuk 6.3, punt 2), niet vanuit deze klasse.
///
/// BEWUST niet via <c>IHttpContextAccessor</c>: een `@rendermode InteractiveServer`-component
/// rendert twee keer - eerst tijdens de statische prerender (met een echte HttpContext), daarna
/// nogmaals zodra de SignalR-circuit het overneemt. Tijdens die tweede render is er geen
/// betrouwbare HttpContext meer (`IHttpContextAccessor.HttpContext` kan dan null zijn), waardoor
/// elk ingelogd interactief scherm crashte. `AuthenticationStateProvider` (via
/// `AddCascadingAuthenticationState()`, Web/Program.cs) is juist ontworpen om de
/// authenticatiestatus correct over beide renderfases heen te laten werken.
///
/// `GetAuthenticationStateAsync()` synchroon opvragen is hier veilig: deze klasse wordt alleen
/// aangeroepen ná `AuthorizeRouteView`/`[Authorize]`, dus de state is dan al opgelost (anders was
/// de gebruiker al naar `RedirectToLogin` gestuurd) - de Task is al voltooid, er wordt niet echt
/// geblokkeerd.
/// </summary>
public sealed class AuthenticationStateCurrentUserContext : ICurrentUserContext
{
    private readonly ClaimsPrincipal _principal;

    public AuthenticationStateCurrentUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        var authenticationState = authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
        _principal = authenticationState.User;
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
