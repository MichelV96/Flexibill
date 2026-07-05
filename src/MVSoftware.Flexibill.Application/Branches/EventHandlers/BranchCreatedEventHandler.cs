using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Branches.Events;

namespace MVSoftware.Flexibill.Application.Branches.EventHandlers;

/// <summary>
/// UC-B2/FO 6.4: zodra een vestiging is aangemaakt, krijgt hij automatisch de simpele
/// standaard-fiateringsflow (1 niveau, elke Fiatteerder van de vestiging).
/// </summary>
public sealed class BranchCreatedEventHandler(IApprovalFlowSettingRepository approvalFlowRepository)
    : INotificationHandler<DomainEventNotification<BranchCreatedEvent>>
{
    public async Task Handle(DomainEventNotification<BranchCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var existingFlow = await approvalFlowRepository.GetStandardFlowAsync(domainEvent.BranchId, cancellationToken);
        if (existingFlow is not null)
        {
            return;
        }

        var standardFlow = ApprovalFlowSetting.CreateDefaultStandardFlow(domainEvent.OrganizationId, domainEvent.BranchId);
        await approvalFlowRepository.SaveAsync(standardFlow, cancellationToken);
    }
}
