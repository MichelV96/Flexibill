using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Approvals;

/// <summary>
/// Aggregate root voor een fiateringsflow (Functioneel Ontwerp, hoofdstuk 6.4; Technisch
/// Ontwerp, hoofdstuk 5.1). Twee varianten van dezelfde vorm:
///
///  - de **standaardflow** van een vestiging (<see cref="SupplierId"/> is null) - elke
///    vestiging krijgt er bij aanmaak automatisch één met precies 1 niveau (UC-B2);
///  - een **leverancier-uitzondering** (<see cref="SupplierId"/> gezet) die de
///    standaardflow van die vestiging overschrijft voor facturen van die leverancier
///    (UC-B5).
///
/// Bewust NIET de verantwoordelijkheid van deze aggregate: controleren of een
/// toegewezen Fiatteerder daadwerkelijk toegang heeft tot de vestiging (dat vergt
/// gegevens van de User-aggregate en gebeurt daarom in de Application-laag - Technisch
/// Ontwerp, hoofdstuk 4.2, "ApprovalFlowSettingValidator").
/// </summary>
public sealed class ApprovalFlowSetting : Entity, ITenantEntity, IBranchScopedEntity, IAuditable
{
    private readonly List<ApprovalFlowLevel> _levels = [];

    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>Null voor de standaardflow van de vestiging; gezet voor een leverancier-uitzondering.</summary>
    public Guid? SupplierId { get; private set; }

    /// <summary>Volgtijdelijk (moet in Sequence-volgorde) of parallel (mag in willekeurige volgorde, FO 6.4).</summary>
    public bool RequiresSequentialApproval { get; private set; }

    public IReadOnlyList<ApprovalFlowLevel> Levels => _levels.OrderBy(l => l.Sequence).ToList().AsReadOnly();

    public bool IsStandardFlow => SupplierId is null;

    private ApprovalFlowSetting() { }

    /// <summary>
    /// UC-B2: de simpele standaardflow (1 niveau, elke Fiatteerder van de vestiging) die
    /// een vestiging automatisch krijgt bij aanmaak (Functioneel Ontwerp, hoofdstuk 6.4).
    /// </summary>
    public static ApprovalFlowSetting CreateDefaultStandardFlow(Guid organizationId, Guid branchId)
    {
        var flow = new ApprovalFlowSetting
        {
            OrganizationId = organizationId,
            BranchId = branchId,
            SupplierId = null,
            RequiresSequentialApproval = false
        };

        flow._levels.Add(ApprovalFlowLevel.Create(sequence: 1, requiredApproverUserId: null, minimumAmount: null));
        return flow;
    }

    /// <summary>UC-B5: maakt een nieuwe leverancier-uitzondering aan voor een vestiging.</summary>
    public static ApprovalFlowSetting CreateSupplierException(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        IReadOnlyList<ApprovalFlowLevelInput> levels,
        bool requiresSequentialApproval)
    {
        var flow = new ApprovalFlowSetting
        {
            OrganizationId = organizationId,
            BranchId = branchId,
            SupplierId = supplierId
        };

        flow.ReplaceLevels(levels, requiresSequentialApproval);
        return flow;
    }

    /// <summary>
    /// UC-B4/UC-B5: vervangt de niveaus van deze flow, bijvoorbeeld om de standaardflow
    /// van een vestiging uit te breiden met een tweede niveau bij een bedragsgrens, of om
    /// een leverancier-uitzondering aan te passen.
    /// </summary>
    public void ReplaceLevels(IReadOnlyList<ApprovalFlowLevelInput> levels, bool requiresSequentialApproval)
    {
        if (levels.Count == 0)
        {
            throw new ArgumentException("An approval flow requires at least one level.", nameof(levels));
        }

        _levels.Clear();
        for (var i = 0; i < levels.Count; i++)
        {
            _levels.Add(ApprovalFlowLevel.Create(sequence: i + 1, levels[i].RequiredApproverUserId, levels[i].MinimumAmount));
        }

        RequiresSequentialApproval = requiresSequentialApproval;
    }

    /// <summary>
    /// Bepaalt, voor een factuurbedrag, welke fiatteerders vereist zijn - direct bruikbaar
    /// als input voor <c>Invoice.SubmitForApproval</c> (Technisch Ontwerp, hoofdstuk 5.1/5.3).
    /// Een niet-null waarde betekent een specifiek vereiste ("vaste") persoon; null betekent
    /// "elke Fiatteerder met toegang tot deze vestiging" (FO 6.4).
    /// </summary>
    public IReadOnlyList<Guid?> ResolveRequiredApprovers(Money invoiceAmount)
    {
        return Levels
            .Where(level => level.AppliesTo(invoiceAmount))
            .Select(level => level.RequiredApproverUserId)
            .ToList();
    }
}

/// <summary>Invoer voor <see cref="ApprovalFlowSetting.ReplaceLevels"/> - nog zonder toegewezen sequence.</summary>
public sealed record ApprovalFlowLevelInput(Guid? RequiredApproverUserId, Money? MinimumAmount);
