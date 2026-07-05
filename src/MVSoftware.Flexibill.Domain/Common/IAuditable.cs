namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Marks an entity whose changes must be written to the append-only audit log
/// (Technisch Ontwerp, hoofdstuk 15) by the AuditInterceptor in Infrastructure.
/// </summary>
public interface IAuditable
{
}
