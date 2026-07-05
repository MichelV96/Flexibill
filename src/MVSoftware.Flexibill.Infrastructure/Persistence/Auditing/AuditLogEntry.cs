using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;

/// <summary>
/// Eén append-only audit-regel (Technisch Ontwerp, hoofdstuk 15): wie, wat, wanneer, oude/
/// nieuwe waarden, en of de wijziging via Web of Worker liep. Geen domain-aggregate - een
/// puur infrastructureel bijeffect, geschreven door <see cref="Interceptors.AuditInterceptor"/>.
/// Alleen scalar-properties van de aggregate-root zelf worden vastgelegd (zie de TODO in
/// AuditInterceptor voor de bekende beperking bij owned child-collecties).
/// </summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public string? ChangedByDisplayName { get; private set; }
    public AuditSource Source { get; private set; }
    public DateTimeOffset TimestampUtc { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Create(
        string entityType,
        Guid entityId,
        AuditAction action,
        Guid? changedByUserId,
        string? changedByDisplayName,
        AuditSource source,
        DateTimeOffset timestampUtc,
        string? oldValuesJson,
        string? newValuesJson) => new()
    {
        EntityType = entityType,
        EntityId = entityId,
        Action = action,
        ChangedByUserId = changedByUserId,
        ChangedByDisplayName = changedByDisplayName,
        Source = source,
        TimestampUtc = timestampUtc,
        OldValuesJson = oldValuesJson,
        NewValuesJson = newValuesJson
    };
}
