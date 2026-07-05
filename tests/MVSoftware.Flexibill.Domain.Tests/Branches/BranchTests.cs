using FluentAssertions;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Branches.Events;
using MVSoftware.Flexibill.Domain.Common;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Branches;

public class BranchTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Create_publiceert_BranchCreatedEvent()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");

        branch.DomainEvents.Should().ContainSingle(e => e is BranchCreatedEvent);
        branch.IsAccountingConnected.Should().BeFalse();
    }

    [Fact]
    public void Create_zonder_naam_is_niet_toegestaan()
    {
        var act = () => Branch.Create(OrganizationId, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConnectAccounting_zet_de_koppeling_actief()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        var now = DateTimeOffset.UtcNow;

        branch.ConnectAccounting(AccountingPackage.ExactOnline, now);

        branch.IsAccountingConnected.Should().BeTrue();
        branch.AccountingPackage.Should().Be(AccountingPackage.ExactOnline);
        branch.AccountingConnectedAtUtc.Should().Be(now);
    }

    [Fact]
    public void RecordAccountingSync_zonder_actieve_koppeling_is_niet_toegestaan()
    {
        var branch = Branch.Create(OrganizationId, "Rotterdam");

        var act = () => branch.RecordAccountingSync(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordAccountingSync_na_verbinden_werkt_de_laatste_synchronisatietijd_bij()
    {
        var branch = Branch.Create(OrganizationId, "Rotterdam");
        branch.ConnectAccounting(AccountingPackage.Snelstart, DateTimeOffset.UtcNow);
        var syncTime = DateTimeOffset.UtcNow;

        branch.RecordAccountingSync(syncTime);

        branch.LastAccountingSyncAtUtc.Should().Be(syncTime);
    }

    [Fact]
    public void DisconnectAccounting_zet_de_koppeling_uit_maar_behoudt_het_gekozen_pakket()
    {
        var branch = Branch.Create(OrganizationId, "Amsterdam");
        branch.ConnectAccounting(AccountingPackage.Yuki, DateTimeOffset.UtcNow);

        branch.DisconnectAccounting();

        branch.IsAccountingConnected.Should().BeFalse();
        branch.AccountingPackage.Should().Be(AccountingPackage.Yuki);
    }
}
