using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Branches.Events;

/// <summary>
/// FO 6.4/UC-B2: een nieuwe vestiging krijgt automatisch de simpele standaard-
/// fiateringsflow. De Application-laag luistert hierop en roept
/// <c>ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId)</c> aan
/// (die aggregate bestaat inmiddels, zie Domain/Approvals) - het aanroepen van die
/// command-handler zelf is nog de volgende stap.
/// </summary>
public sealed record BranchCreatedEvent(Guid BranchId, Guid OrganizationId) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
