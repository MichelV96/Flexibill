using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Invoices;

/// <summary>
/// A single step in an invoice approval flow (Technisch Ontwerp, hoofdstuk 5.1, 6.4).
/// Created when the invoice is submitted for approval, based on the ApprovalFlowSetting
/// resolved by the Application layer (standaardflow of leverancier-uitzondering, FO 6.4).
///
/// <see cref="RequiredApproverUserId"/> is null when any approver with the Approver role
/// for the branch may complete the step (bijv. de "roulerend" fiatteerder uit FO 6.4);
/// it is set when a specific person is required (bijv. de "vaste" fiatteerder).
/// </summary>
public sealed class ApprovalStep : Entity
{
    public int Sequence { get; private set; }
    public Guid? RequiredApproverUserId { get; private set; }
    public ApprovalStepStatus Status { get; private set; } = ApprovalStepStatus.Pending;
    public Guid? DecidedByUserId { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    private ApprovalStep() { }

    internal static ApprovalStep Create(int sequence, Guid? requiredApproverUserId) => new()
    {
        Sequence = sequence,
        RequiredApproverUserId = requiredApproverUserId
    };

    internal void Approve(Guid approverUserId, DateTimeOffset nowUtc)
    {
        if (Status != ApprovalStepStatus.Pending)
        {
            throw new DomainException($"Approval step {Sequence} has already been decided.");
        }

        if (RequiredApproverUserId is { } requiredUserId && requiredUserId != approverUserId)
        {
            throw new DomainException(
                $"Approval step {Sequence} must be completed by a specific approver, not {approverUserId}.");
        }

        Status = ApprovalStepStatus.Approved;
        DecidedByUserId = approverUserId;
        DecidedAtUtc = nowUtc;
    }

    internal void Reject(Guid approverUserId, string reason, DateTimeOffset nowUtc)
    {
        if (Status != ApprovalStepStatus.Pending)
        {
            throw new DomainException($"Approval step {Sequence} has already been decided.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            // FO UC-F5: een reden voor afkeuring is verplicht.
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        Status = ApprovalStepStatus.Rejected;
        DecidedByUserId = approverUserId;
        DecidedAtUtc = nowUtc;
        RejectionReason = reason;
    }
}
