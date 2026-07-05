using MediatR;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Organizations;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Dashboard.Queries;

/// <summary>UC-H1: bouwt het rolafhankelijke dashboard op voor de ingelogde gebruiker.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler(
    ICurrentUserContext currentUser,
    IOrganizationRepository organizationRepository,
    ISupplierRepository supplierRepository) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetByIdAsync(currentUser.OrganizationId, cancellationToken);
        var activeModules = GetActiveModuleNames(organization);

        var isAdministrator = currentUser.HasRole(UserRole.Administrator);
        var isSupplierRole = isAdministrator
            || currentUser.HasRole(UserRole.SupplierManager)
            || currentUser.HasRole(UserRole.SupplierViewer);

        int? suppliersWithMissingData = null;
        int? draftSuppliers = null;
        if (isSupplierRole)
        {
            suppliersWithMissingData = await supplierRepository.CountActiveWithMissingDataAsync(
                currentUser.OrganizationId, cancellationToken);
            draftSuppliers = await supplierRepository.CountDraftAsync(
                currentUser.OrganizationId, cancellationToken);
        }

        return new DashboardDto(
            DisplayName: currentUser.DisplayName,
            ActiveModules: activeModules,
            IsAdministrator: isAdministrator,
            // TODO: koppelen aan de exportlog zodra die bestaat (Technisch Ontwerp, hoofdstuk 12.3).
            FailedExportsCount: isAdministrator ? null : null,
            SuppliersWithMissingDataCount: suppliersWithMissingData,
            DraftSuppliersCount: draftSuppliers,
            // TODO: koppelen aan User-uitnodigingen zodra dat bijgehouden wordt.
            PendingInvitationsCount: null,
            IsApprover: currentUser.HasRole(UserRole.Approver),
            // TODO: koppelen aan een Invoice-repository (ApprovalStep-status per branch).
            PendingApprovalsCount: null,
            IsExpenseApprover: currentUser.HasRole(UserRole.ExpenseApprover),
            PendingExpenseApprovalsCount: null,
            IsPurchaseApprover: currentUser.HasRole(UserRole.PurchaseApprover),
            PendingPurchaseRequestsCount: null);
    }

    private static IReadOnlyCollection<string> GetActiveModuleNames(Organization? organization)
    {
        if (organization is null)
        {
            return [];
        }

        var modules = new List<string>();
        if (organization.IsModuleActive(FlexibillModule.DocumentArchive)) modules.Add("Documentarchief");
        if (organization.IsModuleActive(FlexibillModule.ExpenseProcessing)) modules.Add("Declaratieverwerking");
        if (organization.IsModuleActive(FlexibillModule.PurchaseManagement)) modules.Add("Inkoopmanagement");
        return modules;
    }
}
