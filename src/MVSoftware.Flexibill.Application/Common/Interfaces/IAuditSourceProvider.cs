namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>Technisch Ontwerp, hoofdstuk 15: elke audit-regel legt vast of de wijziging via
/// de Web App of de Worker liep. Web en Worker registreren elk hun eigen vaste waarde.</summary>
public enum AuditSource
{
    Web,
    Worker
}

public interface IAuditSourceProvider
{
    AuditSource Source { get; }
}
