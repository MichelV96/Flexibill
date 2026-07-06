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
/// `GetAuthenticationStateAsync()` wordt bewust pas bij gebruik van een property aangeroepen
/// (niet gecachet in de constructor) zodat altijd de op-dat-moment actuele state wordt gelezen.
/// </summary>
public sealed class AuthenticationStateCurrentUserContext(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserContext
{
    private ClaimsPrincipal Principal =>
        authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult().User;

    public Guid UserId => Guid.Parse(Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current user has no NameIdentifier claim."));

    public Guid OrganizationId => Guid.Parse(Principal.FindFirstValue("organization_id")
        ?? throw new InvalidOperationException("The current user has no organization_id claim."));

    public string DisplayName => Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public IReadOnlyCollection<UserRole> Roles => Principal.FindAll(ClaimTypes.Role)
        .Select(c => Enum.Parse<UserRole>(c.Value))
        .ToList();

    public IReadOnlyCollection<Guid> BranchIds => Principal.FindAll("branch_id")
        .Select(c => Guid.Parse(c.Value))
        .ToList();

    public bool HasRole(UserRole role) => Roles.Contains(role);
}
