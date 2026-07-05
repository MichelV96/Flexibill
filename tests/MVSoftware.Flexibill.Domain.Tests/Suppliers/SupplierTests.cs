using FluentAssertions;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Suppliers;
using MVSoftware.Flexibill.Domain.Suppliers.Events;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Suppliers;

public class SupplierTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void CreateDraft_zet_status_op_Draft()
    {
        var supplier = Supplier.CreateDraft(OrganizationId, "Nieuwe Leverancier XYZ");

        supplier.Status.Should().Be(SupplierStatus.Draft);
        supplier.GetMissingRequiredFields().Should().BeEmpty("een concept-leverancier wordt niet via UC-C6 gesignaleerd");
    }

    [Fact]
    public void CreateDraft_zonder_naam_is_niet_toegestaan()
    {
        var act = () => Supplier.CreateDraft(OrganizationId, "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_zet_concept_om_naar_actief_en_publiceert_event()
    {
        var supplier = Supplier.CreateDraft(OrganizationId, "Nieuwe Leverancier XYZ");

        supplier.Activate();

        supplier.Status.Should().Be(SupplierStatus.Active);
        supplier.DomainEvents.Should().ContainSingle(e => e is SupplierActivatedEvent);
    }

    [Fact]
    public void Activate_van_een_reeds_actieve_leverancier_is_niet_toegestaan()
    {
        var supplier = Supplier.CreateActive(
            OrganizationId, "Kantoorgroothandel BV",
            ChamberOfCommerceNumber.Of("72345678"), vatNumber: null, ibans: null,
            primaryContact: null, address: null, paymentTermDays: 30, category: "Kantoorartikelen",
            defaultGeneralLedgerAccountId: Guid.NewGuid(), defaultCostCenterId: Guid.NewGuid());

        var act = supplier.Activate;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Actieve_leverancier_zonder_kvk_iban_of_grootboekrekening_signaleert_ontbrekende_gegevens()
    {
        var supplier = Supplier.CreateActive(
            OrganizationId, "Nieuwe Leverancier XYZ",
            chamberOfCommerceNumber: null, vatNumber: null, ibans: null,
            primaryContact: null, address: null, paymentTermDays: null, category: null,
            defaultGeneralLedgerAccountId: null, defaultCostCenterId: null);

        var missing = supplier.GetMissingRequiredFields();

        missing.Should().Contain([nameof(supplier.ChamberOfCommerceNumber), nameof(supplier.Ibans),
            nameof(supplier.DefaultGeneralLedgerAccountId), nameof(supplier.DefaultCostCenterId)]);
        supplier.HasMissingRequiredFields().Should().BeTrue();
    }

    [Fact]
    public void Volledig_ingevulde_actieve_leverancier_heeft_geen_ontbrekende_gegevens()
    {
        var supplier = Supplier.CreateActive(
            OrganizationId, "Kantoorgroothandel BV",
            ChamberOfCommerceNumber.Of("72345678"),
            VatNumber.Of("NL123456789B01"),
            [Iban.Of("NL91ABNA0417164300")],
            ContactPerson.Of("Jan de Vries", EmailAddress.Of("jan@kantoorgroothandel.nl")),
            Address.Of("Hoofdstraat", "1", "1234AB", "Amsterdam"),
            paymentTermDays: 30,
            category: "Kantoorartikelen",
            defaultGeneralLedgerAccountId: Guid.NewGuid(),
            defaultCostCenterId: Guid.NewGuid());

        supplier.HasMissingRequiredFields().Should().BeFalse();
    }

    [Fact]
    public void LinkToBranch_is_idempotent()
    {
        var supplier = Supplier.CreateDraft(OrganizationId, "Nieuwe Leverancier XYZ");
        var branchId = Guid.NewGuid();

        supplier.LinkToBranch(branchId);
        supplier.LinkToBranch(branchId);

        supplier.BranchLinks.Should().ContainSingle();
        supplier.IsLinkedToBranch(branchId).Should().BeTrue();
    }

    [Fact]
    public void UnlinkFromBranch_verwijdert_de_koppeling()
    {
        var supplier = Supplier.CreateDraft(OrganizationId, "Nieuwe Leverancier XYZ");
        var branchId = Guid.NewGuid();
        supplier.LinkToBranch(branchId);

        supplier.UnlinkFromBranch(branchId);

        supplier.IsLinkedToBranch(branchId).Should().BeFalse();
    }

    [Fact]
    public void Ongeldig_iban_wordt_geweigerd()
    {
        var act = () => Iban.Of("NL00BANK0000000000");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Geldig_iban_wordt_geaccepteerd()
    {
        var iban = Iban.Of("NL91 ABNA 0417 1643 00");

        iban.Value.Should().Be("NL91ABNA0417164300");
    }

    [Fact]
    public void Kvk_nummer_moet_uit_8_cijfers_bestaan()
    {
        var act = () => ChamberOfCommerceNumber.Of("1234");

        act.Should().Throw<ArgumentException>();
    }
}
