namespace MVSoftware.Flexibill.Contracts.Messages;

/// <summary>
/// Generiek notificatiebericht op de "notifications"-topic (hoofdstuk 12.1, 14).
/// </summary>
public sealed record NotificationRequested(
    Guid OrganizationId,
    Guid RecipientUserId,
    string TemplateName,
    IReadOnlyDictionary<string, string> Parameters);
