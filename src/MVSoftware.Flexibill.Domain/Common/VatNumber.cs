using System.Text.RegularExpressions;

namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Btw-nummer. Valideert strikt op het Nederlandse formaat (NLxxxxxxxxxBxx); accepteert
/// andere landen losser omdat Flexibill zich voor v1 op NL-administraties richt
/// (Functioneel Ontwerp, hoofdstuk 4).
/// </summary>
public sealed partial class VatNumber : ValueObject
{
    public string Value { get; }

    private VatNumber(string value) => Value = value;

    public static VatNumber Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A VAT number is required.", nameof(value));
        }

        var normalized = value.Replace(" ", "").ToUpperInvariant();

        if (normalized.StartsWith("NL", StringComparison.Ordinal) && !DutchFormat().IsMatch(normalized))
        {
            throw new ArgumentException($"'{value}' is not a validly formatted Dutch VAT number.", nameof(value));
        }

        return new VatNumber(normalized);
    }

    [GeneratedRegex("^NL[0-9]{9}B[0-9]{2}$")]
    private static partial Regex DutchFormat();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
