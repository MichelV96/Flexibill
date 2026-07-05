namespace MVSoftware.Flexibill.Application.Dashboard.Queries;

/// <summary>
/// Alle mogelijke dashboardkaarten uit het Functioneel Ontwerp, hoofdstuk 20. Een kaart
/// met een null-waarde betekent "niet van toepassing voor deze gebruiker/rol" en wordt
/// door de UI niet getoond - dat is bewust geen "0", want 0 is een geldige uitkomst.
///
/// Sommige tellingen zijn nog niet aan een repository gekoppeld (er bestaat bijvoorbeeld
/// nog geen Invoice- of exportlog-repository) en staan daarom vast op null met een TODO;
/// zodra die stukken er zijn, vullen we ze hier verder in.
/// </summary>
public sealed record DashboardDto(
    string DisplayName,
    IReadOnlyCollection<string> ActiveModules,
    bool IsAdministrator,
    int? FailedExportsCount,
    int? SuppliersWithMissingDataCount,
    int? DraftSuppliersCount,
    int? PendingInvitationsCount,
    bool IsApprover,
    int? PendingApprovalsCount,
    bool IsExpenseApprover,
    int? PendingExpenseApprovalsCount,
    bool IsPurchaseApprover,
    int? PendingPurchaseRequestsCount);
