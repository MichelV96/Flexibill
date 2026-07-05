namespace MVSoftware.Flexibill.Contracts.Messages;

/// <summary>
/// Gepubliceerd door de Web App na upload van een factuur, geconsumeerd door de
/// Worker (Technisch Ontwerp, hoofdstuk 10.2, 12.1).
/// </summary>
public sealed record InvoiceOcrRequested(
    Guid InvoiceId,
    Guid OrganizationId,
    Guid BranchId,
    string BlobPath);
