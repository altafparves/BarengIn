namespace BarengIn.Domain.ValueObjects;

/// <summary>
/// An immutable monetary amount. Defaults to Indonesian rupiah, which is the only
/// currency the platform handles today; the field exists so that a second currency
/// never becomes a schema change.
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "IDR";

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, "A monetary amount cannot be negative.");
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Splits the amount into <paramref name="parts"/> equal shares and returns one share,
    /// rounded to two decimal places. Rounding means the shares may not sum exactly back to
    /// the original amount; the driver absorbs the difference.
    /// </summary>
    public Money SplitEvenly(int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parts), parts, "A cost must be split into at least one part.");
        }

        return new Money(Math.Round(Amount / parts, 2, MidpointRounding.AwayFromZero), Currency);
    }

    public override string ToString() => FormattableString.Invariant($"{Amount:0.##} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in {Currency} and {other.Currency}.");
        }
    }
}
