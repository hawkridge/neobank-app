using NeoBank.Domain.Enums;

namespace NeoBank.Domain.ValueObjects;

public record Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    public bool IsPositive => Amount > 0;
    public bool IsZeroOrNegative => Amount <= 0;

    public override string ToString() => $"{Amount:F2} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException(
                $"Currency mismatch: cannot operate on {Currency} and {other.Currency}");
    }
}