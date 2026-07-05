using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

public sealed class EfApprovalFlowSettingRepository(FlexibillDbContext dbContext) : IApprovalFlowSettingRepository
{
    public Task<ApprovalFlowSetting?> GetStandardFlowAsync(Guid branchId, CancellationToken cancellationToken) =>
        dbContext.ApprovalFlowSettings.FirstOrDefaultAsync(f => f.BranchId == branchId && f.SupplierId == null, cancellationToken);

    public Task<ApprovalFlowSetting?> GetSupplierExceptionAsync(Guid branchId, Guid supplierId, CancellationToken cancellationToken) =>
        dbContext.ApprovalFlowSettings.FirstOrDefaultAsync(f => f.BranchId == branchId && f.SupplierId == supplierId, cancellationToken);

    public async Task<IReadOnlyList<ApprovalFlowSetting>> GetSupplierExceptionsForBranchAsync(Guid branchId, CancellationToken cancellationToken) =>
        await dbContext.ApprovalFlowSettings
            .Where(f => f.BranchId == branchId && f.SupplierId != null)
            .ToListAsync(cancellationToken);

    /// <summary>Upsert - zowel voor een nieuwe flow als een bijgewerkte bestaande (UC-B4/UC-B5).</summary>
    public async Task SaveAsync(ApprovalFlowSetting flow, CancellationToken cancellationToken)
    {
        var isAlreadyTracked = dbContext.ChangeTracker.Entries<ApprovalFlowSetting>().Any(e => e.Entity.Id == flow.Id);

        if (!isAlreadyTracked)
        {
            var exists = await dbContext.ApprovalFlowSettings.AnyAsync(f => f.Id == flow.Id, cancellationToken);
            if (exists)
            {
                dbContext.ApprovalFlowSettings.Update(flow);
            }
            else
            {
                dbContext.ApprovalFlowSettings.Add(flow);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var flow = await dbContext.ApprovalFlowSettings.FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken);
        if (flow is null)
        {
            return;
        }

        dbContext.ApprovalFlowSettings.Remove(flow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
