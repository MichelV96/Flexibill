namespace MVSoftware.Flexibill.Contracts.Messages;

/// <summary>
/// Domain event-integratiebericht, gepubliceerd zodra een factuur is goedgekeurd
/// (door Web of Worker, zie hoofdstuk 10.3) en getriggerd wordt om te exporteren
/// naar het boekhoudpakket (hoofdstuk 12.1, 13).
/// </summary>
public sealed record InvoiceApproved(
    Guid InvoiceId,
    Guid OrganizationId,
    Guid BranchId);
