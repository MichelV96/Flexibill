using MVSoftware.Flexibill.Domain.Approvals;

namespace MVSoftware.Flexibill.Application.Common.Interfaces;

public interface IApprovalFlowSettingRepository
{
    /// <summary>De standaardflow van een vestiging (SupplierId == null), indien al aangemaakt.</summary>
    Task<ApprovalFlowSetting?> GetStandardFlowAsync(Guid branchId, CancellationToken cancellationToken);

    /// <summary>Een leverancier-uitzondering binnen een vestiging, indien aanwezig (UC-B5).</summary>
    Task<ApprovalFlowSetting?> GetSupplierExceptionAsync(Guid branchId, Guid supplierId, CancellationToken cancellationToken);

    /// <summary>Alle leverancier-uitzonderingen binnen een vestiging (voor het overzicht in het instelscherm).</summary>
    Task<IReadOnlyList<ApprovalFlowSetting>> GetSupplierExceptionsForBranchAsync(Guid branchId, CancellationToken cancellationToken);

    /// <summary>Upsert - zowel voor een nieuwe flow als voor een bijgewerkte bestaande flow.</summary>
    Task SaveAsync(ApprovalFlowSetting flow, CancellationToken cancellationToken);

    Task DeleteAsync(Guid flowId, CancellationToken cancellationToken);
}
