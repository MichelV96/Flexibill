using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Suppliers.Events;

namespace MVSoftware.Flexibill.Domain.Suppliers;

/// <summary>
/// Aggregate root voor een leverancier (Functioneel Ontwerp, hoofdstuk 5; Technisch
/// Ontwerp, hoofdstuk 5.1). Bewaakt:
///  - het concept/actief-onderscheid (FO 5.2, UC-C2/UC-C3);
///  - welke vestigingen ermee mogen werken (FO 5.1, UC-C1) - bepalend voor de
///    branch-gebaseerde zichtbaarheid (Technisch Ontwerp, hoofdstuk 4.2);
///  - welke verplichte gegevens ontbreken voor een actieve leverancier, t.b.v. het
///    dashboard-signaal uit FO UC-C6 (nadrukkelijk geen blokkade - een actieve
///    leverancier met hiaten blijft gewoon bruikbaar).
/// </summary>
public sealed class Supplier : Entity, ITenantEntity, IAuditable
{
    private readonly List<Iban> _ibans = [];
    private readonly List<SupplierBranchLink> _branchLinks = [];

    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SupplierStatus Status { get; private set; }

    public ChamberOfCommerceNumber? ChamberOfCommerceNumber { get; private set; }
    public VatNumber? VatNumber { get; private set; }
    public ContactPerson? PrimaryContact { get; private set; }
    public Address? Address { get; private set; }
    public int? PaymentTermDays { get; private set; }
    public string? Category { get; private set; }

    /// <summary>Voorstel voor factuurregels van deze leverancier (FO 5.1, 6.3).</summary>
    public Guid? DefaultGeneralLedgerAccountId { get; private set; }
    public Guid? DefaultCostCenterId { get; private set; }

    public IReadOnlyCollection<Iban> Ibans => _ibans.AsReadOnly();
    public IReadOnlyCollection<SupplierBranchLink> BranchLinks => _branchLinks.AsReadOnly();

    private Supplier() { }

    /// <summary>UC-C2: minimale aanmaak als concept, bijvoorbeeld tijdens het verwerken van een factuur.</summary>
    public static Supplier CreateDraft(Guid organizationId, string name)
    {
        EnsureName(name);

        return new Supplier
        {
            OrganizationId = organizationId,
            Name = name,
            Status = SupplierStatus.Draft
        };
    }

    /// <summary>UC-C1: volledige aanmaak, direct actief.</summary>
    public static Supplier CreateActive(
        Guid organizationId,
        string name,
        ChamberOfCommerceNumber? chamberOfCommerceNumber,
        VatNumber? vatNumber,
        IEnumerable<Iban>? ibans,
        ContactPerson? primaryContact,
        Address? address,
        int? paymentTermDays,
        string? category,
        Guid? defaultGeneralLedgerAccountId,
        Guid? defaultCostCenterId)
    {
        EnsureName(name);

        var supplier = new Supplier
        {
            OrganizationId = organizationId,
            Name = name,
            Status = SupplierStatus.Active,
            ChamberOfCommerceNumber = chamberOfCommerceNumber,
            VatNumber = vatNumber,
            PrimaryContact = primaryContact,
            Address = address,
            PaymentTermDays = paymentTermDays,
            Category = category,
            DefaultGeneralLedgerAccountId = defaultGeneralLedgerAccountId,
            DefaultCostCenterId = defaultCostCenterId
        };

        if (ibans is not null)
        {
            foreach (var iban in ibans)
            {
                supplier.AddIban(iban);
            }
        }

        return supplier;
    }

    /// <summary>UC-C3: activeert een concept-leverancier. Ontbrekende gegevens blokkeren dit niet (FO UC-C6).</summary>
    public void Activate()
    {
        if (Status != SupplierStatus.Draft)
        {
            throw new DomainException($"Supplier {Id} is already {Status}.");
        }

        Status = SupplierStatus.Active;
        AddDomainEvent(new SupplierActivatedEvent(Id, OrganizationId));
    }

    /// <summary>UC-C4: bewerken is toegestaan ongeacht status (Draft of Active).</summary>
    public void UpdateDetails(
        string name,
        ChamberOfCommerceNumber? chamberOfCommerceNumber,
        VatNumber? vatNumber,
        ContactPerson? primaryContact,
        Address? address,
        int? paymentTermDays,
        string? category,
        Guid? defaultGeneralLedgerAccountId,
        Guid? defaultCostCenterId)
    {
        EnsureName(name);

        Name = name;
        ChamberOfCommerceNumber = chamberOfCommerceNumber;
        VatNumber = vatNumber;
        PrimaryContact = primaryContact;
        Address = address;
        PaymentTermDays = paymentTermDays;
        Category = category;
        DefaultGeneralLedgerAccountId = defaultGeneralLedgerAccountId;
        DefaultCostCenterId = defaultCostCenterId;
    }

    public void AddIban(Iban iban)
    {
        if (_ibans.Contains(iban))
        {
            return;
        }

        _ibans.Add(iban);
    }

    public void RemoveIban(Iban iban) => _ibans.RemoveAll(i => i.Equals(iban));

    /// <summary>FO 5.1: koppelt de leverancier aan een vestiging die ermee mag werken.</summary>
    public void LinkToBranch(Guid branchId)
    {
        if (_branchLinks.Any(l => l.BranchId == branchId))
        {
            return;
        }

        _branchLinks.Add(SupplierBranchLink.Create(branchId));
    }

    public void UnlinkFromBranch(Guid branchId) => _branchLinks.RemoveAll(l => l.BranchId == branchId);

    public bool IsLinkedToBranch(Guid branchId) => _branchLinks.Any(l => l.BranchId == branchId);

    /// <summary>
    /// UC-C6: velden die voor een actieve leverancier ontbreken en op het dashboard
    /// gesignaleerd worden. Voor een concept-leverancier is dit bewust altijd leeg -
    /// dat is al zichtbaar via <see cref="Status"/> zelf (UC-C2/UC-C3).
    /// </summary>
    public IReadOnlyList<string> GetMissingRequiredFields()
    {
        if (Status != SupplierStatus.Active)
        {
            return [];
        }

        var missing = new List<string>();

        if (ChamberOfCommerceNumber is null) missing.Add(nameof(ChamberOfCommerceNumber));
        if (_ibans.Count == 0) missing.Add(nameof(Ibans));
        if (DefaultGeneralLedgerAccountId is null) missing.Add(nameof(DefaultGeneralLedgerAccountId));
        if (DefaultCostCenterId is null) missing.Add(nameof(DefaultCostCenterId));

        return missing;
    }

    public bool HasMissingRequiredFields() => GetMissingRequiredFields().Count > 0;

    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A supplier requires a name.", nameof(name));
        }
    }
}
