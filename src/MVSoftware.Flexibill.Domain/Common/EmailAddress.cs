using System.Text.RegularExpressions;

namespace MVSoftware.Flexibill.Domain.Common;

public sealed partial class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static EmailAddress Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Format().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));
        }

        return new EmailAddress(value.Trim().ToLowerInvariant());
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Format();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
