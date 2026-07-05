using FluentAssertions;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Common;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Approvals;

public class ApprovalFlowSettingTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid BranchId = Guid.NewGuid();

    [Fact]
    public void CreateDefaultStandardFlow_heeft_precies_1_niveau_voor_elke_fiatteerder()
    {
        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId);

        flow.IsStandardFlow.Should().BeTrue();
        flow.Levels.Should().ContainSingle();
        flow.Levels[0].RequiredApproverUserId.Should().BeNull("elke Fiatteerder van de vestiging mag dit niveau afhandelen");
    }

    [Fact]
    public void ResolveRequiredApprovers_voor_de_standaardflow_geeft_1_open_plek()
    {
        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId);

        var approvers = flow.ResolveRequiredApprovers(Money.Of(1240.00m));

        approvers.Should().Equal([null]);
    }

    [Fact]
    public void Uitgebreide_standaardflow_met_bedragsgrens_voegt_pas_een_niveau_toe_boven_de_drempel()
    {
        var beheerderId = Guid.NewGuid();
        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId);

        flow.ReplaceLevels(
            [
                new ApprovalFlowLevelInput(RequiredApproverUserId: null, MinimumAmount: null),
                new ApprovalFlowLevelInput(RequiredApproverUserId: beheerderId, MinimumAmount: Money.Of(1000m))
            ],
            requiresSequentialApproval: false);

        flow.ResolveRequiredApprovers(Money.Of(87.50m)).Should().Equal([null]);
        flow.ResolveRequiredApprovers(Money.Of(1240.00m)).Should().Equal([null, beheerderId]);
    }

    [Fact]
    public void Leverancier_uitzondering_met_vaste_plus_roulerende_fiatteerder()
    {
        var vasteFiatteerder = Guid.NewGuid();

        var flow = ApprovalFlowSetting.CreateSupplierException(
            OrganizationId, BranchId, supplierId: Guid.NewGuid(),
            levels:
            [
                new ApprovalFlowLevelInput(vasteFiatteerder, MinimumAmount: null),
                new ApprovalFlowLevelInput(RequiredApproverUserId: null, MinimumAmount: null)
            ],
            requiresSequentialApproval: true);

        flow.IsStandardFlow.Should().BeFalse();
        flow.RequiresSequentialApproval.Should().BeTrue();
        flow.ResolveRequiredApprovers(Money.Of(6240.00m)).Should().Equal([vasteFiatteerder, null]);
    }

    [Fact]
    public void ReplaceLevels_zonder_niveaus_is_niet_toegestaan()
    {
        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId);

        var act = () => flow.ReplaceLevels([], requiresSequentialApproval: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Levels_staan_altijd_gesorteerd_op_sequence_ongeacht_invoervolgorde()
    {
        var eersteApprover = Guid.NewGuid();
        var tweedeApprover = Guid.NewGuid();
        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, BranchId);

        flow.ReplaceLevels(
            [
                new ApprovalFlowLevelInput(eersteApprover, null),
                new ApprovalFlowLevelInput(tweedeApprover, null)
            ],
            requiresSequentialApproval: true);

        flow.Levels.Select(l => l.Sequence).Should().Equal(1, 2);
        flow.Levels.Select(l => l.RequiredApproverUserId).Should().Equal(eersteApprover, tweedeApprover);
    }
}
