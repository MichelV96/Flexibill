using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

/// <summary>
/// Triviale <see cref="ICurrentUserContext"/>-stub voor plekken zonder ingelogde gebruiker/
/// HttpContext: EF Core-migraties (design-time), de development-seed (<see cref="DbInitializer"/>),
/// en - als "SystemPrincipal" (Technisch Ontwerp, hoofdstuk 6.3 punt 2) - de Worker, die geen
/// HttpContext heeft en dus niet <c>AuthenticationStateCurrentUserContext</c> kan gebruiken. Zonder
/// argumenten (zoals de Worker hem registreert) is dit bewust geen specifieke tenant: prima
/// voor tenant-onafhankelijk werk zoals de <see cref="OutboxProcessor"/>, maar nog niet geschikt
/// zodra de Worker zelf tenant-gebonden business-commands gaat uitvoeren (dan is per-bericht
/// context nodig, gebaseerd op het Service Bus-bericht).
/// </summary>
public sealed class SystemCurrentUserContext(Guid organizationId = default, IReadOnlyCollection<Guid>? branchIds = null) : ICurrentUserContext
{
    public Guid UserId => Guid.Empty;
    public Guid OrganizationId { get; } = organizationId;
    public string DisplayName => "System";
    public IReadOnlyCollection<UserRole> Roles => [];
    public IReadOnlyCollection<Guid> BranchIds { get; } = branchIds ?? [];

    public bool HasRole(UserRole role) => false;
}
