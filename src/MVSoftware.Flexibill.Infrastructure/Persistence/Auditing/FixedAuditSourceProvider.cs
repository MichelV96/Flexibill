using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;

/// <summary>Triviale implementatie - Web en Worker registreren elk hun eigen vaste waarde in Program.cs.</summary>
public sealed class FixedAuditSourceProvider(AuditSource source) : IAuditSourceProvider
{
    public AuditSource Source { get; } = source;
}
