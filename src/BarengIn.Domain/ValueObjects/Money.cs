namespace BarengIn.Domain.ValueObjects;

/// <summary>
/// An immutable monetary amount in a specific currency. Arithmetic never mutates
/// the instance; it always returns a new <see cref="Money"/>.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    private readonly decimal amount;
    private readonly string currency;

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        this.amount = amount;
        this.currency = currency;
    }

    /// <summary>Returns a new instance holding the sum of both amounts. Currencies must match.</summary>
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!string.Equals(currency, other.currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot add {other.currency} to {currency}: currencies must match.");
        }

        return new Money(amount + other.amount, currency);
    }

    /// <summary>Returns a new instance holding an equal share of this amount, split across <paramref name="parts"/>.</summary>
    public Money Split(int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parts), parts, "Parts must be greater than zero.");
        }

        return new Money(amount / parts, currency);
    }

    public override string ToString() => $"{amount} {currency}";

    public bool Equals(Money? other) =>
        other is not null && amount == other.amount && string.Equals(currency, other.currency, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(amount, currency);
}
