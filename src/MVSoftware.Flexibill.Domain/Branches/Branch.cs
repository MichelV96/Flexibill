using MVSoftware.Flexibill.Domain.Branches.Events;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Branches;

/// <summary>
/// Aggregate root voor een vestiging (Functioneel Ontwerp, hoofdstuk 3.2). Bewaakt de
/// boekhoudkoppeling (FO 6.6) - fiateringsflows zijn bewust een eigen aggregate
/// (ApprovalFlowSetting, nog te bouwen) en horen daarom niet hier in.
/// </summary>
public sealed class Branch : Entity, ITenantEntity, IAuditable
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PostalAddress? Address { get; private set; }

    public AccountingPackage? AccountingPackage { get; private set; }
    public bool IsAccountingConnected { get; private set; }
    public DateTimeOffset? AccountingConnectedAtUtc { get; private set; }
    public DateTimeOffset? LastAccountingSyncAtUtc { get; private set; }

    private Branch() { }

    /// <summary>UC-B2: Beheerder maakt een nieuwe vestiging aan.</summary>
    public static Branch Create(Guid organizationId, string name, PostalAddress? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A branch requires a name.", nameof(name));
        }

        var branch = new Branch
        {
            OrganizationId = organizationId,
            Name = name,
            Address = address
        };

        branch.AddDomainEvent(new BranchCreatedEvent(branch.Id, organizationId));
        return branch;
    }

    public void UpdateDetails(string name, PostalAddress? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A branch requires a name.", nameof(name));
        }

        Name = name;
        Address = address;
    }

    /// <summary>UC-B3, stap 1-2: koppelt de vestiging aan een boekhoudpakket.</summary>
    public void ConnectAccounting(AccountingPackage package, DateTimeOffset nowUtc)
    {
        AccountingPackage = package;
        IsAccountingConnected = true;
        AccountingConnectedAtUtc = nowUtc;
    }

    public void DisconnectAccounting()
    {
        IsAccountingConnected = false;
    }

    /// <summary>UC-B3, stap 3: bijgewerkt na een geslaagde stamdatasynchronisatie.</summary>
    public void RecordAccountingSync(DateTimeOffset nowUtc)
    {
        if (!IsAccountingConnected)
        {
            throw new DomainException($"Branch {Id} has no active accounting connection to sync.");
        }

        LastAccountingSyncAtUtc = nowUtc;
    }
}
