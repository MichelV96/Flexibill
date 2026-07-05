using System.Numerics;
using System.Text.RegularExpressions;

namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// IBAN value object met een MOD-97 checksum-validatie (Technisch Ontwerp, hoofdstuk 5.2).
/// </summary>
public sealed partial class Iban : ValueObject
{
    public string Value { get; }

    private Iban(string value) => Value = value;

    public static Iban Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An IBAN is required.", nameof(value));
        }

        var normalized = value.Replace(" ", "").ToUpperInvariant();

        if (!IbanFormat().IsMatch(normalized))
        {
            throw new ArgumentException($"'{value}' is not a validly formatted IBAN.", nameof(value));
        }

        if (!HasValidChecksum(normalized))
        {
            throw new ArgumentException($"'{value}' does not have a valid IBAN checksum.", nameof(value));
        }

        return new Iban(normalized);
    }

    private static bool HasValidChecksum(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var numeric = new System.Text.StringBuilder();
        foreach (var c in rearranged)
        {
            numeric.Append(char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString());
        }

        return BigInteger.Parse(numeric.ToString()) % 97 == 1;
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")]
    private static partial Regex IbanFormat();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
