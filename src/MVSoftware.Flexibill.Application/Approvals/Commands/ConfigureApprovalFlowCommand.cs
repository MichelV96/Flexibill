using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Application.Approvals.Commands;

/// <summary>Eén niveau zoals ingevoerd in het scherm "Fiateringsflow instellen" (nog zonder toegewezen sequence).</summary>
public sealed record ApprovalFlowLevelDto(Guid? RequiredApproverUserId, decimal? MinimumAmount);

/// <summary>
/// UC-B4 (standaardflow van een vestiging) en UC-B5 (leverancier-uitzondering) delen
/// hetzelfde command: <paramref name="SupplierId"/> is null voor de standaardflow,
/// gezet voor een uitzondering (Functioneel Ontwerp, hoofdstuk 6.4).
/// </summary>
public sealed record ConfigureApprovalFlowCommand(
    Guid BranchId,
    Guid? SupplierId,
    IReadOnlyList<ApprovalFlowLevelDto> Levels,
    bool RequiresSequentialApproval) : IRequest<Result>;

public sealed class ConfigureApprovalFlowCommandValidator : AbstractValidator<ConfigureApprovalFlowCommand>
{
    public ConfigureApprovalFlowCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Levels).NotEmpty().WithMessage("Een fiateringsflow heeft minimaal 1 niveau nodig.");
        RuleForEach(x => x.Levels).ChildRules(level =>
        {
            level.RuleFor(l => l.MinimumAmount).GreaterThanOrEqualTo(0m).When(l => l.MinimumAmount is not null);
        });
    }
}

public sealed class ConfigureApprovalFlowCommandHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository,
    IUserRepository userRepository,
    IApprovalFlowSettingRepository approvalFlowRepository) : IRequestHandler<ConfigureApprovalFlowCommand, Result>
{
    public async Task<Result> Handle(ConfigureApprovalFlowCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null || branch.OrganizationId != currentUser.OrganizationId)
        {
            return Result.Failure("De vestiging is niet gevonden.");
        }

        // Technisch Ontwerp, hoofdstuk 4.2: "elke toegewezen Fiatteerder moet toegang hebben tot de
        // vestiging" is een cross-aggregate check (User-gegevens) en hoort daarom hier, niet in de
        // ApprovalFlowSetting-aggregate zelf.
        foreach (var level in request.Levels)
        {
            if (level.RequiredApproverUserId is not { } approverId)
            {
                continue; // null = "elke Fiatteerder van de vestiging", niets te valideren.
            }

            var approver = await userRepository.GetByIdAsync(approverId, cancellationToken);
            if (approver is null || approver.OrganizationId != currentUser.OrganizationId)
            {
                return Result.Failure($"Gebruiker {approverId} bestaat niet binnen deze organisatie.");
            }

            if (!approver.HasRole(UserRole.Approver))
            {
                return Result.Failure($"{approver.DisplayName} heeft niet de rol Fiatteerder.");
            }

            if (!approver.HasAccessToBranch(request.BranchId))
            {
                // FO wireframe "Fiateringsflow instellen" toont dit exact als waarschuwing bij het niveau.
                return Result.Failure($"{approver.DisplayName} heeft geen toegang tot vestiging '{branch.Name}'.");
            }
        }

        var levelInputs = request.Levels
            .Select(l => new ApprovalFlowLevelInput(
                l.RequiredApproverUserId,
                l.MinimumAmount is { } amount ? Money.Of(amount) : null))
            .ToList();

        var flow = request.SupplierId is null
            ? await GetOrCreateStandardFlowAsync(branch.OrganizationId, request.BranchId, cancellationToken)
            : await GetOrCreateSupplierExceptionAsync(branch.OrganizationId, request.BranchId, request.SupplierId.Value, cancellationToken);

        flow.ReplaceLevels(levelInputs, request.RequiresSequentialApproval);
        await approvalFlowRepository.SaveAsync(flow, cancellationToken);

        return Result.Success();
    }

    private async Task<ApprovalFlowSetting> GetOrCreateStandardFlowAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken)
    {
        var existing = await approvalFlowRepository.GetStandardFlowAsync(branchId, cancellationToken);
        return existing ?? ApprovalFlowSetting.CreateDefaultStandardFlow(organizationId, branchId);
    }

    private async Task<ApprovalFlowSetting> GetOrCreateSupplierExceptionAsync(
        Guid organizationId, Guid branchId, Guid supplierId, CancellationToken cancellationToken)
    {
        var existing = await approvalFlowRepository.GetSupplierExceptionAsync(branchId, supplierId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // Tijdelijk 1 open niveau; ReplaceLevels() in Handle() zet direct de echte niveaus.
        return ApprovalFlowSetting.CreateSupplierException(
            organizationId, branchId, supplierId,
            levels: [new ApprovalFlowLevelInput(null, null)],
            requiresSequentialApproval: false);
    }
}
