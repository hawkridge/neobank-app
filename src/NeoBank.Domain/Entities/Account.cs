using NeoBank.Domain.Common;
using NeoBank.Domain.Enums;
using NeoBank.Domain.Events;

namespace NeoBank.Domain.Entities;

public class Account : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Account() { }

    public Account(Guid userId, Currency currency)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Currency = currency;
        Balance = 0;
        AccountNumber = GenerateAccountNumber();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AccountCreated(Id, UserId, Currency));
    }

    public Result Debit(decimal amount)
    {
        if (!IsActive)
            return Result.Failure("Account.Inactive");

        if (amount <= 0)
            return Result.Failure("Account.InvalidAmount");

        if (Balance < amount)
            return Result.Failure("Account.InsufficientFunds");

        Balance -= amount;
        return Result.Success();
    }

    public Result Credit(decimal amount)
    {
        if (!IsActive)
            return Result.Failure("Account.Inactive");

        if (amount <= 0)
            return Result.Failure("Account.InvalidAmount");

        Balance += amount;
        return Result.Success();
    }

    public Result Close()
    {
        if (!IsActive)
            return Result.Failure("Account.AlreadyClosed");

        if (Balance > 0)
            return Result.Failure("Account.HasBalance");

        IsActive = false;
        return Result.Success();
    }

    private static string GenerateAccountNumber()
    {
        // UA + 27 digits — IBAN-like format
        var part1 = Random.Shared.NextInt64(100_000_000_000_000L, 999_999_999_999_999L); // 15 digits
        var part2 = Random.Shared.Next(100_000_000, 999_999_999);                         // 9 digits
        var part3 = Random.Shared.Next(100, 999);                                         // 3 digits
        return $"UA{part1}{part2}{part3}";
    }
}