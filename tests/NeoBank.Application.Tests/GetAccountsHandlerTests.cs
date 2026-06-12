using NeoBank.Application.Queries.GetAccounts;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Enums;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class GetAccountsHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();

    private GetAccountsHandler CreateHandler() => new(_accounts);

    [Fact]
    public async Task Handle_UserWithAccounts_ReturnsMappedDtos()
    {
        var userId = Guid.NewGuid();
        var userAccounts = new List<Account>
        {
            new(userId, Currency.UAH),
            new(userId, Currency.USD),
        };
        _accounts.GetByUserIdAsync(userId).Returns(userAccounts);

        var result = await CreateHandler().HandleAsync(
            new GetAccountsQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Currency == Currency.UAH);
        Assert.Contains(result, a => a.Currency == Currency.USD);
    }

    [Fact]
    public async Task Handle_UserWithNoAccounts_ReturnsEmptyList()
    {
        _accounts.GetByUserIdAsync(Arg.Any<Guid>()).Returns(new List<Account>());

        var result = await CreateHandler().HandleAsync(
            new GetAccountsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }
}