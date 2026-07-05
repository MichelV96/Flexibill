using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Approvals.Queries;

public sealed record ApprovalFlowLevelViewDto(
    int Sequence,
    Guid? RequiredApproverUserId,
    string? RequiredApproverName,
    decimal? MinimumAmount,
    bool ApproverLacksBranchAccess);

public sealed record ApprovalFlowViewDto(
    bool Exists,
    bool RequiresSequentialApproval,
    IReadOnlyList<ApprovalFlowLevelViewDto> Levels);

public sealed record SupplierExceptionViewDto(Guid SupplierId, string SupplierName, ApprovalFlowViewDto Flow);

public sealed record ApproverOptionDto(Guid Id, string DisplayName, bool HasBranchAccess);

public sealed record SupplierOptionDto(Guid Id, string Name);

public sealed record ApprovalFlowOverviewDto(
    Guid BranchId,
    string BranchName,
    ApprovalFlowViewDto StandardFlow,
    IReadOnlyList<SupplierExceptionViewDto> SupplierExceptions,
    IReadOnlyList<ApproverOptionDto> AvailableApprovers,
    IReadOnlyList<SupplierOptionDto> SuppliersWithoutException);

/// <summary>Alle gegevens voor het scherm "Fiateringsflow instellen" (FO 6.4, UC-B4/UC-B5) in één keer.</summary>
public sealed record GetApprovalFlowOverviewQuery(Guid BranchId) : IRequest<ApprovalFlowOverviewDto?>;

public sealed class GetApprovalFlowOverviewQueryHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository,
    IApprovalFlowSettingRepository approvalFlowRepository,
    IUserRepository userRepository,
    ISupplierRepository supplierRepository) : IRequestHandler<GetApprovalFlowOverviewQuery, ApprovalFlowOverviewDto?>
{
    public async Task<ApprovalFlowOverviewDto?> Handle(GetApprovalFlowOverviewQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null || branch.OrganizationId != currentUser.OrganizationId)
        {
            return null;
        }

        var allUsers = await userRepository.GetAllAsync(currentUser.OrganizationId, cancellationToken);
        var approvers = allUsers.Where(u => u.HasRole(UserRole.Approver)).ToList();
        var approverNamesById = approvers.ToDictionary(u => u.Id, u => u.DisplayName);

        var standardFlowSetting = await approvalFlowRepository.GetStandardFlowAsync(request.BranchId, cancellationToken);
        var standardFlow = ToViewDto(standardFlowSetting, approverNamesById, approvers);

        var exceptionSettings = await approvalFlowRepository.GetSupplierExceptionsForBranchAsync(request.BranchId, cancellationToken);
        var supplierExceptions = new List<SupplierExceptionViewDto>();
        foreach (var exceptionSetting in exceptionSettings)
        {
            var supplier = await supplierRepository.GetByIdAsync(exceptionSetting.SupplierId!.Value, cancellationToken);
            supplierExceptions.Add(new SupplierExceptionViewDto(
                exceptionSetting.SupplierId!.Value,
                supplier?.Name ?? "(onbekende leverancier)",
                ToViewDto(exceptionSetting, approverNamesById, approvers)));
        }

        var branchSuppliers = await supplierRepository.GetByBranchAsync(request.BranchId, cancellationToken);
        var suppliersWithoutException = branchSuppliers
            .Where(s => exceptionSettings.All(e => e.SupplierId != s.Id))
            .Select(s => new SupplierOptionDto(s.Id, s.Name))
            .OrderBy(s => s.Name)
            .ToList();

        var availableApprovers = approvers
            .Select(u => new ApproverOptionDto(u.Id, u.DisplayName, u.HasAccessToBranch(request.BranchId)))
            .OrderBy(a => a.DisplayName)
            .ToList();

        return new ApprovalFlowOverviewDto(
            branch.Id, branch.Name, standardFlow, supplierExceptions, availableApprovers, suppliersWithoutException);
    }

    private static ApprovalFlowViewDto ToViewDto(
        ApprovalFlowSetting? flow, IReadOnlyDictionary<Guid, string> approverNamesById, IReadOnlyList<User> approvers)
    {
        if (flow is null)
        {
            return new ApprovalFlowViewDto(Exists: false, RequiresSequentialApproval: false, Levels: []);
        }

        var levels = flow.Levels.Select(level =>
        {
            string? name = null;
            var lacksAccess = false;

            if (level.RequiredApproverUserId is { } approverId)
            {
                name = approverNamesById.GetValueOrDefault(approverId, "(onbekende gebruiker)");
                var approver = approvers.FirstOrDefault(u => u.Id == approverId);
                lacksAccess = approver is null || !approver.HasAccessToBranch(flow.BranchId);
            }

            return new ApprovalFlowLevelViewDto(level.Sequence, level.RequiredApproverUserId, name, level.MinimumAmount?.Amount, lacksAccess);
        }).ToList();

        return new ApprovalFlowViewDto(Exists: true, flow.RequiresSequentialApproval, levels);
    }
}
