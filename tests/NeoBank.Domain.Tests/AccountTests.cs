using NeoBank.Domain.Entities;
using NeoBank.Domain.Enums;

namespace NeoBank.Domain.Tests;

public class AccountTests
{
    private static Account CreateAccount(decimal initialBalance = 0m)
    {
        var account = new Account(Guid.NewGuid(), Currency.UAH);
        if (initialBalance > 0)
            account.Credit(initialBalance);
        return account;
    }

    // --- Debit ---

    [Fact]
    public void Debit_SufficientBalance_Succeeds()
    {
        var account = CreateAccount(initialBalance: 1000m);

        var result = account.Debit(300m);

        Assert.True(result.IsSuccess);
        Assert.Equal(700m, account.Balance);
    }

    [Fact]
    public void Debit_ExactBalance_Succeeds()
    {
        var account = CreateAccount(initialBalance: 100m);

        var result = account.Debit(100m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Debit_InsufficientBalance_ReturnsFailure()
    {
        var account = CreateAccount(initialBalance: 100m);

        var result = account.Debit(500m);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.InsufficientFunds", result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Debit_InvalidAmount_ReturnsFailure(decimal amount)
    {
        var account = CreateAccount(initialBalance: 1000m);

        var result = account.Debit(amount);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.InvalidAmount", result.Error);
    }

    [Fact]
    public void Debit_InactiveAccount_ReturnsFailure()
    {
        var account = CreateAccount(initialBalance: 0m);
        account.Close();

        var result = account.Debit(100m);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.Inactive", result.Error);
    }

    // --- Credit ---

    [Fact]
    public void Credit_ValidAmount_IncreasesBalance()
    {
        var account = CreateAccount();

        var result = account.Credit(500m);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, account.Balance);
    }

    [Fact]
    public void Credit_InactiveAccount_ReturnsFailure()
    {
        var account = CreateAccount(initialBalance: 0m);
        account.Close();

        var result = account.Credit(500m);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.Inactive", result.Error);
    }

    // --- Close ---

    [Fact]
    public void Close_ZeroBalance_Succeeds()
    {
        var account = CreateAccount(initialBalance: 0m);

        var result = account.Close();

        Assert.True(result.IsSuccess);
        Assert.False(account.IsActive);
    }

    [Fact]
    public void Close_WithBalance_ReturnsFailure()
    {
        var account = CreateAccount(initialBalance: 500m);

        var result = account.Close();

        Assert.True(result.IsFailure);
        Assert.Equal("Account.HasBalance", result.Error);
    }

    [Fact]
    public void Close_AlreadyClosed_ReturnsFailure()
    {
        var account = CreateAccount(initialBalance: 0m);
        account.Close();

        var result = account.Close();

        Assert.True(result.IsFailure);
        Assert.Equal("Account.AlreadyClosed", result.Error);
    }
}