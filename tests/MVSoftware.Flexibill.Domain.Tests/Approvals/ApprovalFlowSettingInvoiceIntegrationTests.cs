using FluentAssertions;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Invoices;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Approvals;

/// <summary>
/// Laat zien dat <see cref="ApprovalFlowSetting.ResolveRequiredApprovers"/> rechtstreeks
/// bruikbaar is als invoer voor <see cref="Invoice.SubmitForApproval"/> - de twee
/// aggregates zijn onafhankelijk, maar hun contracten sluiten naadloos op elkaar aan
/// (Technisch Ontwerp, hoofdstuk 5.1).
/// </summary>
public class ApprovalFlowSettingInvoiceIntegrationTests
{
    [Fact]
    public void Een_uitzonderingsflow_bepaalt_precies_de_approvalsteps_op_de_factuur()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var vasteFiatteerder = Guid.NewGuid();

        var flow = ApprovalFlowSetting.CreateSupplierException(
            organizationId, branchId, supplierId,
            levels:
            [
                new ApprovalFlowLevelInput(vasteFiatteerder, MinimumAmount: null),
                new ApprovalFlowLevelInput(RequiredApproverUserId: null, MinimumAmount: null)
            ],
            requiresSequentialApproval: true);

        var invoice = Invoice.Create(organizationId, branchId, supplierId, "EUR", supplierRequiresApproval: false);
        invoice.SetHeaderDetails("2026-0345", new DateOnly(2026, 6, 28), null, Money.Of(6240.00m), Money.Of(1310.40m));
        var line = invoice.AddLine("Grootzakelijk energiecontract", 1, Money.Of(6240.00m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");

        var requiredApprovers = flow.ResolveRequiredApprovers(invoice.TotalAmountExclVat);
        invoice.SubmitForApproval(requiredApprovers, flow.RequiresSequentialApproval);

        invoice.ApprovalSteps.Should().HaveCount(2);
        invoice.ApprovalSteps.First(s => s.Sequence == 1).RequiredApproverUserId.Should().Be(vasteFiatteerder);
        invoice.ApprovalSteps.First(s => s.Sequence == 2).RequiredApproverUserId.Should().BeNull();

        invoice.Approve(vasteFiatteerder, stepSequence: 1);
        invoice.Status.Should().Be(InvoiceStatus.PendingApproval);

        var roulerendeFiatteerder = Guid.NewGuid();
        invoice.Approve(roulerendeFiatteerder, stepSequence: 2);
        invoice.Status.Should().Be(InvoiceStatus.Approved);
    }

    [Fact]
    public void Een_kleine_factuur_onder_de_bedragsgrens_heeft_maar_1_approvalstep_nodig()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var beheerderId = Guid.NewGuid();

        var flow = ApprovalFlowSetting.CreateDefaultStandardFlow(organizationId, branchId);
        flow.ReplaceLevels(
            [
                new ApprovalFlowLevelInput(RequiredApproverUserId: null, MinimumAmount: null),
                new ApprovalFlowLevelInput(beheerderId, MinimumAmount: Money.Of(1000m))
            ],
            requiresSequentialApproval: false);

        var invoice = Invoice.Create(organizationId, branchId, supplierId, "EUR", supplierRequiresApproval: false);
        invoice.SetHeaderDetails("2026-0342", new DateOnly(2026, 6, 29), null, Money.Of(87.50m), Money.Of(18.38m));
        var line = invoice.AddLine("Visitekaartjes", 1, Money.Of(87.50m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");

        var requiredApprovers = flow.ResolveRequiredApprovers(invoice.TotalAmountExclVat);
        invoice.SubmitForApproval(requiredApprovers, flow.RequiresSequentialApproval);

        invoice.ApprovalSteps.Should().ContainSingle("het bedrag zit onder de drempel van de tweede fiatteerder");
    }
}
