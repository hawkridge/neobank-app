using NeoBank.Application.Queries.GetAccountById;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Enums;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class GetAccountByIdHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();

    private GetAccountByIdHandler CreateHandler() => new(_accounts);

    [Fact]
    public async Task Handle_ExistingAccount_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var account = new Account(userId, Currency.UAH);
        account.Credit(500m);

        _accounts.GetByIdAsync(account.Id).Returns(account);

        var result = await CreateHandler().HandleAsync(
            new GetAccountByIdQuery(account.Id, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.Balance);
        Assert.Equal(Currency.UAH, result.Value.Currency);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsFailure()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>()).Returns((Account?)null);

        var result = await CreateHandler().HandleAsync(
            new GetAccountByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.NotFound", result.Error);
    }

    [Fact]
    public async Task Handle_AccountBelongsToDifferentUser_ReturnsAccessDenied()
    {
        var account = new Account(Guid.NewGuid(), Currency.USD);
        _accounts.GetByIdAsync(account.Id).Returns(account);

        var differentUserId = Guid.NewGuid();
        var result = await CreateHandler().HandleAsync(
            new GetAccountByIdQuery(account.Id, differentUserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.AccessDenied", result.Error);
    }
}