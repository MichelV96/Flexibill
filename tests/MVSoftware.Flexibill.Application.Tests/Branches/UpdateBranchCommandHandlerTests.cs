using FluentAssertions;
using MVSoftware.Flexibill.Application.Branches.Commands;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Branches;
using NSubstitute;
using Xunit;

namespace MVSoftware.Flexibill.Application.Tests.Branches;

public class UpdateBranchCommandHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();

    private UpdateBranchCommandHandler CreateHandler() => new(_currentUser, _branchRepository);

    public UpdateBranchCommandHandlerTests()
    {
        _currentUser.OrganizationId.Returns(OrganizationId);
    }

    [Fact]
    public async Task Onbekende_vestiging_geeft_een_foutmelding()
    {
        var branchId = Guid.NewGuid();
        _branchRepository.GetByIdAsync(branchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var command = new UpdateBranchCommand(branchId, "Rotterdam", null, null, null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _branchRepository.DidNotReceive().UpdateAsync(Arg.Any<Branch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Vestiging_van_een_andere_organisatie_geeft_een_foutmelding()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Amsterdam");
        branch.ClearDomainEvents();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var command = new UpdateBranchCommand(branch.Id, "Rotterdam", null, null, null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Naam_en_adres_worden_bijgewerkt_en_opgeslagen()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ClearDomainEvents();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var command = new UpdateBranchCommand(branch.Id, "Rotterdam", "Coolsingel", "1", "3011AD", "Rotterdam");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        branch.Name.Should().Be("Rotterdam");
        branch.Address.Should().NotBeNull();
        branch.Address!.City.Should().Be("Rotterdam");
        await _branchRepository.Received(1).UpdateAsync(branch, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ontbrekend_straat_of_plaats_laat_het_adres_leeg()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ClearDomainEvents();
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var command = new UpdateBranchCommand(branch.Id, "Amsterdam", null, null, null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        branch.Address.Should().BeNull();
    }
}
