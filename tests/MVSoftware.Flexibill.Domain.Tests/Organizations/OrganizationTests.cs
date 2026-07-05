using FluentAssertions;
using MVSoftware.Flexibill.Domain.Organizations;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Organizations;

public class OrganizationTests
{
    [Fact]
    public void Create_start_zonder_actieve_modules()
    {
        var organization = Organization.Create("Kantoorgroothandel Groep BV");

        organization.ActiveModules.Should().Be(FlexibillModule.None);
        organization.IsModuleActive(FlexibillModule.DocumentArchive).Should().BeFalse();
    }

    [Fact]
    public void ActivateModule_zet_alleen_de_gevraagde_module_aan()
    {
        var organization = Organization.Create("Kantoorgroothandel Groep BV");

        organization.ActivateModule(FlexibillModule.DocumentArchive);

        organization.IsModuleActive(FlexibillModule.DocumentArchive).Should().BeTrue();
        organization.IsModuleActive(FlexibillModule.ExpenseProcessing).Should().BeFalse();
        organization.IsModuleActive(FlexibillModule.PurchaseManagement).Should().BeFalse();
    }

    [Fact]
    public void Meerdere_modules_kunnen_tegelijk_actief_zijn()
    {
        var organization = Organization.Create("Kantoorgroothandel Groep BV");

        organization.ActivateModule(FlexibillModule.DocumentArchive);
        organization.ActivateModule(FlexibillModule.ExpenseProcessing);

        organization.IsModuleActive(FlexibillModule.DocumentArchive).Should().BeTrue();
        organization.IsModuleActive(FlexibillModule.ExpenseProcessing).Should().BeTrue();
        organization.IsModuleActive(FlexibillModule.PurchaseManagement).Should().BeFalse();
    }

    [Fact]
    public void DeactivateModule_zet_alleen_die_module_weer_uit()
    {
        var organization = Organization.Create("Kantoorgroothandel Groep BV");
        organization.ActivateModule(FlexibillModule.DocumentArchive);
        organization.ActivateModule(FlexibillModule.ExpenseProcessing);

        organization.DeactivateModule(FlexibillModule.DocumentArchive);

        organization.IsModuleActive(FlexibillModule.DocumentArchive).Should().BeFalse();
        organization.IsModuleActive(FlexibillModule.ExpenseProcessing).Should().BeTrue();
    }

    [Fact]
    public void Create_zonder_naam_is_niet_toegestaan()
    {
        var act = () => Organization.Create("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeSubscriptionPlan_wijzigt_het_abonnement()
    {
        var organization = Organization.Create("Kantoorgroothandel Groep BV");

        organization.ChangeSubscriptionPlan(SubscriptionPlan.FixedPrice);

        organization.SubscriptionPlan.Should().Be(SubscriptionPlan.FixedPrice);
    }
}
