namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Thrown when an aggregate invariant or state-transition rule is violated
/// (e.g. approving an invoice that is not pending approval). These represent
/// programming errors in the Application layer, not user-facing validation
/// errors - those are caught earlier by FluentValidation (Technisch Ontwerp,
/// hoofdstuk 6.2) and never reach the domain.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
