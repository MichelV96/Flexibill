using FluentAssertions;
using MVSoftware.Flexibill.Application.Approvals.Commands;
using MVSoftware.Flexibill.Application.Approvals.Queries;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Approvals;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Suppliers;
using MVSoftware.Flexibill.Domain.Users;
using NSubstitute;
using Xunit;

namespace MVSoftware.Flexibill.Application.Tests.Approvals;

public class GetApprovalFlowOverviewQueryHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IApprovalFlowSettingRepository _flowRepository = Substitute.For<IApprovalFlowSettingRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();

    public GetApprovalFlowOverviewQueryHandlerTests() => _currentUser.OrganizationId.Returns(OrganizationId);

    private GetApprovalFlowOverviewQueryHandler CreateHandler() =>
        new(_currentUser, _branchRepository, _flowRepository, _userRepository, _supplierRepository);

    [Fact]
    public async Task Onbekende_of_andere_organisatie_geeft_null_terug()
    {
        _branchRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await CreateHandler().Handle(new GetApprovalFlowOverviewQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Waarschuwt_als_de_toegewezen_fiatteerder_geen_toegang_meer_heeft_tot_de_vestiging()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ClearDomainEvents();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var approverZonderToegang = User.Invite(
            OrganizationId, EmailAddress.Of("tom@flexibill.nl"), "Tom Bakker",
            roles: [UserRole.Approver], branchIds: []); // geen toegang tot de vestiging
        _userRepository.GetAllAsync(OrganizationId, Arg.Any<CancellationToken>())
            .Returns(new List<User> { approverZonderToegang });

        var standardFlow = ApprovalFlowSetting.CreateDefaultStandardFlow(OrganizationId, branch.Id);
        standardFlow.ReplaceLevels([new ApprovalFlowLevelInput(approverZonderToegang.Id, null)], requiresSequentialApproval: false);
        _flowRepository.GetStandardFlowAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(standardFlow);
        _flowRepository.GetSupplierExceptionsForBranchAsync(branch.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApprovalFlowSetting>());
        _supplierRepository.GetByBranchAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(new List<Supplier>());

        var overview = await CreateHandler().Handle(new GetApprovalFlowOverviewQuery(branch.Id), CancellationToken.None);

        overview.Should().NotBeNull();
        overview!.StandardFlow.Levels.Single().ApproverLacksBranchAccess.Should().BeTrue();
    }

    [Fact]
    public async Task Leverancier_uitzonderingen_krijgen_de_juiste_leveranciersnaam_mee()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ClearDomainEvents();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _userRepository.GetAllAsync(OrganizationId, Arg.Any<CancellationToken>()).Returns(new List<User>());
        _flowRepository.GetStandardFlowAsync(branch.Id, Arg.Any<CancellationToken>()).Returns((ApprovalFlowSetting?)null);

        var supplier = Supplier.CreateActive(
            OrganizationId, "Grootzakelijk Energiecontract BV",
            chamberOfCommerceNumber: null, vatNumber: null, ibans: null, primaryContact: null, address: null,
            paymentTermDays: null, category: null, defaultGeneralLedgerAccountId: null, defaultCostCenterId: null);
        supplier.LinkToBranch(branch.Id);
        _supplierRepository.GetByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.GetByBranchAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(new List<Supplier> { supplier });

        var exceptionFlow = ApprovalFlowSetting.CreateSupplierException(
            OrganizationId, branch.Id, supplier.Id, [new ApprovalFlowLevelInput(null, null)], requiresSequentialApproval: false);
        _flowRepository.GetSupplierExceptionsForBranchAsync(branch.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ApprovalFlowSetting> { exceptionFlow });

        var overview = await CreateHandler().Handle(new GetApprovalFlowOverviewQuery(branch.Id), CancellationToken.None);

        overview!.SupplierExceptions.Should().ContainSingle(e => e.SupplierName == "Grootzakelijk Energiecontract BV");
        overview.SuppliersWithoutException.Should().BeEmpty("de enige leverancier heeft al een uitzondering");
    }
}

public class DeleteSupplierExceptionCommandHandlerTests
{
    private readonly IApprovalFlowSettingRepository _flowRepository = Substitute.For<IApprovalFlowSettingRepository>();

    [Fact]
    public async Task Niet_bestaande_uitzondering_geeft_een_foutmelding()
    {
        _flowRepository.GetSupplierExceptionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ApprovalFlowSetting?)null);

        var result = await new DeleteSupplierExceptionCommandHandler(_flowRepository)
            .Handle(new DeleteSupplierExceptionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Bestaande_uitzondering_wordt_verwijderd()
    {
        var branchId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var flow = ApprovalFlowSetting.CreateSupplierException(
            Guid.NewGuid(), branchId, supplierId, [new ApprovalFlowLevelInput(null, null)], false);
        _flowRepository.GetSupplierExceptionAsync(branchId, supplierId, Arg.Any<CancellationToken>()).Returns(flow);

        var result = await new DeleteSupplierExceptionCommandHandler(_flowRepository)
            .Handle(new DeleteSupplierExceptionCommand(branchId, supplierId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _flowRepository.Received(1).DeleteAsync(flow.Id, Arg.Any<CancellationToken>());
    }
}
