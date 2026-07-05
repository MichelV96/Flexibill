using FluentAssertions;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Invoices;
using MVSoftware.Flexibill.Domain.Invoices.Events;
using Xunit;

namespace MVSoftware.Flexibill.Domain.Tests.Invoices;

public class InvoiceTests
{
    private static Invoice CreateCodableInvoice(bool supplierRequiresApproval = false)
    {
        var invoice = Invoice.Create(
            organizationId: Guid.NewGuid(),
            branchId: Guid.NewGuid(),
            supplierId: Guid.NewGuid(),
            currency: "EUR",
            supplierRequiresApproval: supplierRequiresApproval);

        if (supplierRequiresApproval)
        {
            invoice.OnSupplierActivated();
        }

        invoice.SetHeaderDetails(
            invoiceNumber: "2026-0341",
            invoiceDate: new DateOnly(2026, 6, 28),
            dueDate: new DateOnly(2026, 7, 28),
            totalAmountExclVat: Money.Of(1024.79m),
            totalVatAmount: Money.Of(215.21m));

        return invoice;
    }

    [Fact]
    public void Create_met_concept_leverancier_start_in_AwaitingSupplierApproval()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "EUR", supplierRequiresApproval: true);

        invoice.Status.Should().Be(InvoiceStatus.AwaitingSupplierApproval);
    }

    [Fact]
    public void SetHeaderDetails_voor_activatie_van_concept_leverancier_is_niet_toegestaan()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "EUR", supplierRequiresApproval: true);

        var act = () => invoice.SetHeaderDetails("2026-0001", DateOnly.FromDateTime(DateTime.Today), null, Money.Zero(), Money.Zero());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubmitForApproval_faalt_als_niet_alle_regels_gecodeerd_zijn()
    {
        var invoice = CreateCodableInvoice();
        invoice.AddLine("Printerpapier A4", 1, Money.Of(1024.79m));

        var act = () => invoice.SubmitForApproval([null], requiresSequentialApproval: false);

        act.Should().Throw<DomainException>().WithMessage("*coded*");
    }

    [Fact]
    public void SubmitForApproval_faalt_als_regelbedragen_niet_aansluiten_op_het_totaal()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Printerpapier A4", 1, Money.Of(500m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");

        var act = () => invoice.SubmitForApproval([null], requiresSequentialApproval: false);

        act.Should().Throw<DomainException>().WithMessage("*reconcile*");
    }

    [Fact]
    public void Standaardflow_met_1_fiatteerder_keurt_de_factuur_goed()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Printerpapier A4", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([null], requiresSequentialApproval: false);

        var approver = Guid.NewGuid();
        invoice.Approve(approver, stepSequence: 1);

        invoice.Status.Should().Be(InvoiceStatus.Approved);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceApprovedEvent);
    }

    [Fact]
    public void Uitzonderingsflow_met_2_fiatteerders_is_pas_goedgekeurd_na_beide_stappen()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Grootzakelijk energiecontract", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");

        var vasteFiatteerder = Guid.NewGuid();
        invoice.SubmitForApproval([vasteFiatteerder, null], requiresSequentialApproval: true);

        invoice.Approve(vasteFiatteerder, stepSequence: 1);
        invoice.Status.Should().Be(InvoiceStatus.PendingApproval, "er is nog een tweede stap nodig");

        var roulerendeFiatteerder = Guid.NewGuid();
        invoice.Approve(roulerendeFiatteerder, stepSequence: 2);

        invoice.Status.Should().Be(InvoiceStatus.Approved);
    }

    [Fact]
    public void Volgtijdelijke_flow_staat_niet_toe_dat_stap_2_voor_stap_1_wordt_afgehandeld()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Grootzakelijk energiecontract", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([Guid.NewGuid(), null], requiresSequentialApproval: true);

        var act = () => invoice.Approve(Guid.NewGuid(), stepSequence: 2);

        act.Should().Throw<DomainException>().WithMessage("*sequential*");
    }

    [Fact]
    public void Afkeuren_van_een_van_de_stappen_keurt_de_hele_factuur_af()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Grootzakelijk energiecontract", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([Guid.NewGuid(), null], requiresSequentialApproval: false);

        invoice.Reject(Guid.NewGuid(), stepSequence: 1, reason: "Bedrag klopt niet met de offerte");

        invoice.Status.Should().Be(InvoiceStatus.Rejected);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceRejectedEvent);
    }

    [Fact]
    public void Afkeuren_zonder_reden_is_niet_toegestaan()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Printerpapier A4", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([null], requiresSequentialApproval: false);

        var act = () => invoice.Reject(Guid.NewGuid(), stepSequence: 1, reason: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Na_afkeuring_kan_de_factuur_opnieuw_gecodeerd_en_ingediend_worden()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Printerpapier A4", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([null], requiresSequentialApproval: false);
        invoice.Reject(Guid.NewGuid(), stepSequence: 1, reason: "Onjuiste kostenplaats");

        invoice.ReopenForCoding();

        invoice.Status.Should().Be(InvoiceStatus.Coding);
        invoice.ApprovalSteps.Should().BeEmpty();
    }

    [Fact]
    public void Volledige_levenscyclus_van_indienen_tot_archiveren()
    {
        var invoice = CreateCodableInvoice();
        var line = invoice.AddLine("Printerpapier A4", 1, Money.Of(1024.79m));
        invoice.CodeLine(line.Id, Guid.NewGuid(), Guid.NewGuid(), "21%");
        invoice.SubmitForApproval([null], requiresSequentialApproval: false);
        invoice.Approve(Guid.NewGuid(), stepSequence: 1);

        invoice.MarkProcessed(externalBookingReference: "EXACT-BOOKING-12345");
        invoice.Status.Should().Be(InvoiceStatus.Processed);

        invoice.Archive();
        invoice.Status.Should().Be(InvoiceStatus.Archived);
    }
}
