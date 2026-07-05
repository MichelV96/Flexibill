using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Approvals;

/// <summary>
/// Eén niveau in een fiateringsflow (Functioneel Ontwerp, hoofdstuk 6.4). Een niveau
/// geldt altijd (<see cref="MinimumAmount"/> is null - bijv. de "vaste" fiatteerder in
/// een leverancier-uitzondering) of alleen boven een bedragsdrempel (bijv. "Marieke Jansen
/// - alleen bij bedrag > € 1.000" in een standaardflow met meerdere niveaus).
///
/// <see cref="RequiredApproverUserId"/> is null voor "elke Fiatteerder met toegang tot
/// deze vestiging" (het simpele standaardgeval, of de "roulerend" fiatteerder in een
/// uitzonderingsflow); anders voor een specifiek vereiste ("vaste") persoon.
/// </summary>
public sealed class ApprovalFlowLevel : Entity
{
    public int Sequence { get; private set; }
    public Guid? RequiredApproverUserId { get; private set; }
    public Money? MinimumAmount { get; private set; }

    private ApprovalFlowLevel() { }

    internal static ApprovalFlowLevel Create(int sequence, Guid? requiredApproverUserId, Money? minimumAmount) => new()
    {
        Sequence = sequence,
        RequiredApproverUserId = requiredApproverUserId,
        MinimumAmount = minimumAmount
    };

    internal bool AppliesTo(Money invoiceAmount) => MinimumAmount is null || invoiceAmount.Amount >= MinimumAmount.Amount;
}
