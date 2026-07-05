using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Invoices.Events;

namespace MVSoftware.Flexibill.Domain.Invoices;

/// <summary>
/// Aggregate root voor een inkoopfactuur (Technisch Ontwerp, hoofdstuk 5.1, 5.3, 5.4).
///
/// Bewaakt zelf:
///  - de statusmachine (5.3);
///  - dat codering (grootboekrekening/kostenplaats/btw) op regelniveau gebeurt, niet op
///    het totaal (FO 6.3);
///  - dat de som van de regelbedragen overeenkomt met het factuurtotaal, met een kleine
///    afrondingsmarge, voordat de factuur ter goedkeuring aangeboden mag worden;
///  - de goedkeuringsstappen (<see cref="ApprovalStep"/>), inclusief zowel de simpele
///    standaardflow (1 stap) als de uitzonderingsflow met meerdere stappen (FO 6.4).
///
/// Wat deze aggregate bewust NIET doet: bepalen WIE de vereiste goedkeurders zijn voor
/// een factuur (dat is de verantwoordelijkheid van ApprovalFlowSetting + de Application-
/// laag, die de resolved lijst van approvers meegeeft aan <see cref="SubmitForApproval"/>),
/// en het daadwerkelijk exporteren naar het boekhoudpakket (dat gebeurt door een
/// IAccountingConnector in Infrastructure, getriggerd door <see cref="InvoiceApprovedEvent"/>).
/// </summary>
public sealed class Invoice : Entity, ITenantEntity, IBranchScopedEntity, IAuditable
{
    private readonly List<InvoiceLine> _lines = [];
    private readonly List<ApprovalStep> _approvalSteps = [];

    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid SupplierId { get; private set; }

    public string? InvoiceNumber { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }

    /// <summary>Het totaalbedrag excl. btw zoals herkend/ingevoerd op de factuurkop.</summary>
    public Money TotalAmountExclVat { get; private set; } = Money.Zero();
    public Money TotalVatAmount { get; private set; } = Money.Zero();
    public Money TotalAmountInclVat => TotalAmountExclVat + TotalVatAmount;

    public InvoiceStatus Status { get; private set; }
    public bool RequiresSequentialApproval { get; private set; }
    public string? ExternalBookingReference { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();

    private Invoice() { }

    /// <summary>
    /// Maakt een nieuwe factuur aan. <paramref name="supplierRequiresApproval"/> geeft aan
    /// of de leverancier nog een concept is (FO 5.2) - de factuur start dan in
    /// <see cref="InvoiceStatus.AwaitingSupplierApproval"/> in plaats van meteen
    /// <see cref="InvoiceStatus.Coding"/> (UC-F7).
    /// </summary>
    public static Invoice Create(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        string currency,
        bool supplierRequiresApproval)
    {
        return new Invoice
        {
            OrganizationId = organizationId,
            BranchId = branchId,
            SupplierId = supplierId,
            TotalAmountExclVat = Money.Zero(currency),
            TotalVatAmount = Money.Zero(currency),
            Status = supplierRequiresApproval ? InvoiceStatus.AwaitingSupplierApproval : InvoiceStatus.Coding
        };
    }

    /// <summary>
    /// Zet de header-gegevens zoals herkend door OCR of handmatig ingevoerd
    /// (Technisch Ontwerp, hoofdstuk 10.2). Mag alleen terwijl er nog gecodeerd wordt.
    /// </summary>
    public void SetHeaderDetails(string invoiceNumber, DateOnly invoiceDate, DateOnly? dueDate, Money totalAmountExclVat, Money totalVatAmount)
    {
        EnsureStatus(InvoiceStatus.Coding, nameof(SetHeaderDetails));

        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        TotalAmountExclVat = totalAmountExclVat;
        TotalVatAmount = totalVatAmount;
    }

    /// <summary>
    /// UC-C3 / UC-F7: zodra de gekoppelde (concept-)leverancier is geactiveerd, stroomt de
    /// factuur automatisch door naar coderen.
    /// </summary>
    public void OnSupplierActivated()
    {
        EnsureStatus(InvoiceStatus.AwaitingSupplierApproval, nameof(OnSupplierActivated));
        Status = InvoiceStatus.Coding;
    }

    /// <summary>Voegt een factuurregel toe (automatisch via OCR, of handmatig, FO 6.2/6.3).</summary>
    public InvoiceLine AddLine(string description, decimal quantity, Money unitPrice, decimal? ocrConfidence = null)
    {
        EnsureStatus(InvoiceStatus.Coding, nameof(AddLine));

        var line = InvoiceLine.Create(description, quantity, unitPrice, ocrConfidence);
        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Codeert (of herstelt de codering van) een specifieke regel - zowel het automatische
    /// OCR-pad als het handmatig-coderen-scherm (FO 6.3) lopen via deze methode.
    /// </summary>
    public void CodeLine(Guid lineId, Guid generalLedgerAccountId, Guid costCenterId, string vatCode)
    {
        EnsureStatus(InvoiceStatus.Coding, nameof(CodeLine));

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Invoice line {lineId} does not belong to invoice {Id}.");

        line.ApplyCoding(generalLedgerAccountId, costCenterId, vatCode);
    }

    /// <summary>
    /// Som van de regelbedragen (excl. btw), vergeleken met <see cref="TotalAmountExclVat"/>
    /// met een kleine afrondingsmarge (Technisch Ontwerp, hoofdstuk 5.4).
    /// </summary>
    public bool LinesReconcileWithTotal()
    {
        if (_lines.Count == 0)
        {
            return false;
        }

        var currency = TotalAmountExclVat.Currency;
        var sum = _lines.Aggregate(Money.Zero(currency), (running, line) => running + line.Amount);
        return sum.IsApproximately(TotalAmountExclVat);
    }

    /// <summary>
    /// UC-F1/UC-F3/UC-F4: dient de factuur in ter goedkeuring. <paramref name="requiredApproverUserIds"/>
    /// is de door de Application-laag (op basis van ApprovalFlowSetting, FO 6.4) opgeloste lijst van
    /// stappen - null op een positie betekent "elke Fiatteerder met toegang tot deze vestiging".
    /// </summary>
    public void SubmitForApproval(IReadOnlyList<Guid?> requiredApproverUserIds, bool requiresSequentialApproval)
    {
        EnsureStatus(InvoiceStatus.Coding, nameof(SubmitForApproval));

        if (requiredApproverUserIds.Count == 0)
        {
            throw new ArgumentException("At least one approver is required.", nameof(requiredApproverUserIds));
        }

        if (!_lines.All(l => l.IsCoded))
        {
            throw new DomainException("All invoice lines must be coded before submitting for approval.");
        }

        if (!LinesReconcileWithTotal())
        {
            throw new DomainException(
                "The sum of the invoice lines does not reconcile with the invoice total; cannot submit for approval.");
        }

        _approvalSteps.Clear();
        for (var i = 0; i < requiredApproverUserIds.Count; i++)
        {
            _approvalSteps.Add(ApprovalStep.Create(sequence: i + 1, requiredApproverUserIds[i]));
        }

        RequiresSequentialApproval = requiresSequentialApproval;
        Status = InvoiceStatus.PendingApproval;
        AddDomainEvent(new InvoiceSubmittedForApprovalEvent(Id, OrganizationId, BranchId));
    }

    /// <summary>
    /// UC-F3/UC-F4: registreert de goedkeuring van één stap. Zodra alle stappen zijn
    /// goedgekeurd, gaat de factuur naar <see cref="InvoiceStatus.Approved"/> en wordt
    /// <see cref="InvoiceApprovedEvent"/> gepubliceerd.
    /// </summary>
    public void Approve(Guid approverUserId, int stepSequence, DateTimeOffset? nowUtc = null)
    {
        EnsureStatus(InvoiceStatus.PendingApproval, nameof(Approve));

        var step = GetStep(stepSequence);

        if (RequiresSequentialApproval && _approvalSteps.Any(s => s.Sequence < stepSequence && s.Status == ApprovalStepStatus.Pending))
        {
            throw new DomainException(
                $"Approval step {stepSequence} cannot be completed before earlier steps in a sequential flow.");
        }

        step.Approve(approverUserId, nowUtc ?? DateTimeOffset.UtcNow);

        if (_approvalSteps.All(s => s.Status == ApprovalStepStatus.Approved))
        {
            Status = InvoiceStatus.Approved;
            AddDomainEvent(new InvoiceApprovedEvent(Id, OrganizationId, BranchId));
        }
    }

    /// <summary>
    /// UC-F5: keurt de factuur af. Eén afkeuring is genoeg om de hele factuur af te keuren,
    /// ongeacht eventuele eerdere goedkeuringen op andere stappen (FO UC-F4).
    /// </summary>
    public void Reject(Guid approverUserId, int stepSequence, string reason, DateTimeOffset? nowUtc = null)
    {
        EnsureStatus(InvoiceStatus.PendingApproval, nameof(Reject));

        var step = GetStep(stepSequence);
        step.Reject(approverUserId, reason, nowUtc ?? DateTimeOffset.UtcNow);

        Status = InvoiceStatus.Rejected;
        AddDomainEvent(new InvoiceRejectedEvent(Id, OrganizationId, BranchId, reason));
    }

    /// <summary>UC-F5: na afkeuring kan de indiener de factuur opnieuw coderen en indienen.</summary>
    public void ReopenForCoding()
    {
        EnsureStatus(InvoiceStatus.Rejected, nameof(ReopenForCoding));

        _approvalSteps.Clear();
        Status = InvoiceStatus.Coding;
    }

    /// <summary>
    /// UC-F8: wordt aangeroepen nadat de export naar het boekhoudpakket is gelukt
    /// (door de Application-laag, ongeacht of dat vanuit de Web App of de Worker
    /// gebeurde - Technisch Ontwerp, hoofdstuk 10.3).
    /// </summary>
    public void MarkProcessed(string externalBookingReference)
    {
        EnsureStatus(InvoiceStatus.Approved, nameof(MarkProcessed));

        if (string.IsNullOrWhiteSpace(externalBookingReference))
        {
            throw new ArgumentException("An external booking reference is required.", nameof(externalBookingReference));
        }

        ExternalBookingReference = externalBookingReference;
        Status = InvoiceStatus.Processed;
        AddDomainEvent(new InvoiceProcessedEvent(Id, OrganizationId, BranchId, externalBookingReference));
    }

    public void Archive()
    {
        EnsureStatus(InvoiceStatus.Processed, nameof(Archive));
        Status = InvoiceStatus.Archived;
    }

    private ApprovalStep GetStep(int sequence) =>
        _approvalSteps.FirstOrDefault(s => s.Sequence == sequence)
            ?? throw new DomainException($"Invoice {Id} has no approval step with sequence {sequence}.");

    private void EnsureStatus(InvoiceStatus expected, string operation)
    {
        if (Status != expected)
        {
            throw new DomainException(
                $"Cannot perform '{operation}' on invoice {Id}: expected status '{expected}' but was '{Status}'.");
        }
    }
}
