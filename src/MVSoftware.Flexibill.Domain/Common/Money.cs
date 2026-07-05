namespace MVSoftware.Flexibill.Domain.Common;

/// <summary>
/// Immutable money value object (Technisch Ontwerp, hoofdstuk 5.2).
/// Amount is rounded to two decimals; Currency is an ISO 4217 code (e.g. "EUR").
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency = "EUR") => new(0m, NormalizeCurrency(currency));

    public static Money Of(decimal amount, string currency = "EUR")
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero), normalizedCurrency);
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }

        return currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Whether this amount is within <paramref name="tolerance"/> of <paramref name="other"/>.
    /// Used to reconcile the sum of invoice lines against the invoice total, allowing for
    /// small rounding differences (Technisch Ontwerp, hoofdstuk 5.4 / 6.3).
    /// </summary>
    public bool IsApproximately(Money other, decimal tolerance = 0.02m)
    {
        EnsureSameCurrency(other);
        return Math.Abs(Amount - other.Amount) <= tolerance;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in different currencies ({Currency} and {other.Currency}).");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
}
