using System.Text.RegularExpressions;

namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>Nederlands KVK-nummer: exact 8 cijfers (Technisch Ontwerp, hoofdstuk 5.2).</summary>
public sealed partial class ChamberOfCommerceNumber : ValueObject
{
    public string Value { get; }

    private ChamberOfCommerceNumber(string value) => Value = value;

    public static ChamberOfCommerceNumber Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Format().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid Chamber of Commerce number (8 digits expected).", nameof(value));
        }

        return new ChamberOfCommerceNumber(value);
    }

    [GeneratedRegex("^[0-9]{8}$")]
    private static partial Regex Format();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
