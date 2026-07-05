namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Base class for immutable value objects (Money, Iban, VatNumber, ...).
/// Equality is based on components, not identity (Technisch Ontwerp, hoofdstuk 5.2).
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(17, (hash, c) => HashCode.Combine(hash, c));
}
