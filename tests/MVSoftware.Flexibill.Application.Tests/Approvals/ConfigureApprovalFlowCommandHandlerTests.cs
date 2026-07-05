using FluentAssertions;
using MVSoftware.Flexibill.Application.Approvals.Commands;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;
using NSubstitute;
using Xunit;

namespace MVSoftware.Flexibill.Application.Tests.Approvals;

public class ConfigureApprovalFlowCommandHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid BranchId = Guid.NewGuid();

    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IApprovalFlowSettingRepository _flowRepository = Substitute.For<IApprovalFlowSettingRepository>();

    private ConfigureApprovalFlowCommandHandler CreateHandler() =>
        new(_currentUser, _branchRepository, _userRepository, _flowRepository);

    private Branch CreateBranchInOrganization()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ClearDomainEvents();
        return branch;
    }

    public ConfigureApprovalFlowCommandHandlerTests()
    {
        _currentUser.OrganizationId.Returns(OrganizationId);
    }

    [Fact]
    public async Task Onbekende_vestiging_geeft_een_foutmelding()
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);
        var command = new ConfigureApprovalFlowCommand(BranchId, null, [new ApprovalFlowLevelDto(null, null)], false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Fiatteerder_zonder_toegang_tot_de_vestiging_wordt_geweigerd()
    {
        var branch = CreateBranchInOrganization();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var approverWithoutAccess = User.Invite(
            OrganizationId, EmailAddress.Of("tom@flexibill.nl"), "Tom Bakker",
            roles: [UserRole.Approver], branchIds: []);
        _userRepository.GetByIdAsync(approverWithoutAccess.Id, Arg.Any<CancellationToken>()).Returns(approverWithoutAccess);

        var command = new ConfigureApprovalFlowCommand(
            branch.Id, SupplierId: null, [new ApprovalFlowLevelDto(approverWithoutAccess.Id, null)], RequiresSequentialApproval: false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("geen toegang");
        await _flowRepository.DidNotReceive().SaveAsync(Arg.Any<ApprovalFlowSetting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gebruiker_zonder_Approver_rol_wordt_geweigerd_ook_met_branch_toegang()
    {
        var branch = CreateBranchInOrganization();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var supplierManagerZonderApproverRol = User.Invite(
            OrganizationId, EmailAddress.Of("lisa@flexibill.nl"), "Lisa Meijer",
            roles: [UserRole.SupplierManager], branchIds: [branch.Id]);
        _userRepository.GetByIdAsync(supplierManagerZonderApproverRol.Id, Arg.Any<CancellationToken>())
            .Returns(supplierManagerZonderApproverRol);

        var command = new ConfigureApprovalFlowCommand(
            branch.Id, null, [new ApprovalFlowLevelDto(supplierManagerZonderApproverRol.Id, null)], false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Fiatteerder");
    }

    [Fact]
    public async Task Geldige_standaardflow_wordt_opgeslagen()
    {
        var branch = CreateBranchInOrganization();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var approver = User.Invite(
            OrganizationId, EmailAddress.Of("jan@flexibill.nl"), "Jan de Vries",
            roles: [UserRole.Approver], branchIds: [branch.Id]);
        _userRepository.GetByIdAsync(approver.Id, Arg.Any<CancellationToken>()).Returns(approver);
        _flowRepository.GetStandardFlowAsync(branch.Id, Arg.Any<CancellationToken>()).Returns((ApprovalFlowSetting?)null);

        var command = new ConfigureApprovalFlowCommand(
            branch.Id, SupplierId: null, [new ApprovalFlowLevelDto(approver.Id, null)], RequiresSequentialApproval: false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _flowRepository.Received(1).SaveAsync(
            Arg.Is<ApprovalFlowSetting>(f => f.IsStandardFlow && f.Levels.Single().RequiredApproverUserId == approver.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Niveau_zonder_specifieke_fiatteerder_hoeft_niet_gevalideerd_te_worden()
    {
        var branch = CreateBranchInOrganization();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _flowRepository.GetStandardFlowAsync(branch.Id, Arg.Any<CancellationToken>()).Returns((ApprovalFlowSetting?)null);

        var command = new ConfigureApprovalFlowCommand(
            branch.Id, SupplierId: null, [new ApprovalFlowLevelDto(RequiredApproverUserId: null, MinimumAmount: null)], false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leverancier_uitzondering_wordt_apart_van_de_standaardflow_opgeslagen()
    {
        var branch = CreateBranchInOrganization();
        var supplierId = Guid.NewGuid();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _flowRepository.GetSupplierExceptionAsync(branch.Id, supplierId, Arg.Any<CancellationToken>())
            .Returns((ApprovalFlowSetting?)null);

        var vasteFiatteerder = User.Invite(
            OrganizationId, EmailAddress.Of("jan@flexibill.nl"), "Jan de Vries",
            roles: [UserRole.Approver], branchIds: [branch.Id]);
        _userRepository.GetByIdAsync(vasteFiatteerder.Id, Arg.Any<CancellationToken>()).Returns(vasteFiatteerder);

        var command = new ConfigureApprovalFlowCommand(
            branch.Id, supplierId,
            [
                new ApprovalFlowLevelDto(vasteFiatteerder.Id, null),
                new ApprovalFlowLevelDto(null, null)
            ],
            RequiresSequentialApproval: true);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _flowRepository.Received(1).SaveAsync(
            Arg.Is<ApprovalFlowSetting>(f => !f.IsStandardFlow && f.SupplierId == supplierId && f.Levels.Count == 2),
            Arg.Any<CancellationToken>());
    }
}
