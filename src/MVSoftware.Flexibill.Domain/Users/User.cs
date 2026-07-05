using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Users;

/// <summary>
/// Aggregate root voor een gebruiker (Functioneel Ontwerp, hoofdstuk 3.3/3.4).
/// Rollen zijn combineerbaar en worden altijd expliciet toegekend - er is bewust
/// geen impliciete "Administrator heeft alle rechten"-regel (FO 3.4).
/// </summary>
public sealed class User : Entity, ITenantEntity, IAuditable
{
    private readonly List<UserRole> _roles = [];
    private readonly List<Guid> _branchIds = [];

    public Guid OrganizationId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<Guid> BranchIds => _branchIds.AsReadOnly();

    private User() { }

    /// <summary>UC-B1: Beheerder nodigt een gebruiker uit met rol(len) en vestiging(en).</summary>
    public static User Invite(Guid organizationId, EmailAddress email, string displayName, IEnumerable<UserRole> roles, IEnumerable<Guid> branchIds)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        var user = new User
        {
            OrganizationId = organizationId,
            Email = email,
            DisplayName = displayName
        };

        user._roles.AddRange(roles.Distinct());
        user._branchIds.AddRange(branchIds.Distinct());
        return user;
    }

    public bool HasRole(UserRole role) => _roles.Contains(role);

    public bool HasAccessToBranch(Guid branchId) => _branchIds.Contains(branchId);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>Wijzigt de rollen en/of vestigingstoegang van een bestaande gebruiker (FO 4.3).</summary>
    public void UpdateRolesAndBranches(IEnumerable<UserRole> roles, IEnumerable<Guid> branchIds)
    {
        _roles.Clear();
        _roles.AddRange(roles.Distinct());

        _branchIds.Clear();
        _branchIds.AddRange(branchIds.Distinct());
    }

    public void RecordLogin(DateTimeOffset nowUtc) => LastLoginAtUtc = nowUtc;
}
