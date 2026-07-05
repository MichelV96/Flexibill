using FluentAssertions;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Users;

public class UserTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private static User Invite(IEnumerable<UserRole>? roles = null, IEnumerable<Guid>? branchIds = null) =>
        User.Invite(
            OrganizationId,
            EmailAddress.Of("lisa@flexibill.nl"),
            "Lisa Meijer",
            roles ?? [],
            branchIds ?? []);

    [Fact]
    public void Invite_start_actief_en_zonder_eerdere_login()
    {
        var user = Invite();

        user.IsActive.Should().BeTrue();
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Fact]
    public void Invite_zonder_weergavenaam_is_niet_toegestaan()
    {
        var act = () => User.Invite(OrganizationId, EmailAddress.Of("lisa@flexibill.nl"), " ", [], []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Een_gebruiker_kan_meerdere_rollen_tegelijk_hebben()
    {
        var user = Invite(roles: [UserRole.SupplierManager, UserRole.Approver]);

        user.HasRole(UserRole.SupplierManager).Should().BeTrue();
        user.HasRole(UserRole.Approver).Should().BeTrue();
        user.HasRole(UserRole.Administrator).Should().BeFalse();
    }

    [Fact]
    public void HasAccessToBranch_klopt_met_de_toegewezen_vestigingen()
    {
        var branchId = Guid.NewGuid();
        var user = Invite(branchIds: [branchId]);

        user.HasAccessToBranch(branchId).Should().BeTrue();
        user.HasAccessToBranch(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_en_Activate_schakelen_IsActive()
    {
        var user = Invite();

        user.Deactivate();
        user.IsActive.Should().BeFalse();

        user.Activate();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateRolesAndBranches_vervangt_de_volledige_set()
    {
        var oldBranch = Guid.NewGuid();
        var newBranch = Guid.NewGuid();
        var user = Invite(roles: [UserRole.SupplierViewer], branchIds: [oldBranch]);

        user.UpdateRolesAndBranches([UserRole.Approver, UserRole.ExpenseApprover], [newBranch]);

        user.HasRole(UserRole.SupplierViewer).Should().BeFalse();
        user.HasRole(UserRole.Approver).Should().BeTrue();
        user.HasRole(UserRole.ExpenseApprover).Should().BeTrue();
        user.HasAccessToBranch(oldBranch).Should().BeFalse();
        user.HasAccessToBranch(newBranch).Should().BeTrue();
    }

    [Fact]
    public void RecordLogin_zet_het_laatste_inlogmoment()
    {
        var user = Invite();
        var now = DateTimeOffset.UtcNow;

        user.RecordLogin(now);

        user.LastLoginAtUtc.Should().Be(now);
    }
}
