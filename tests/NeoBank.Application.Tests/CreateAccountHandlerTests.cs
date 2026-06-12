using NeoBank.Application.Commands.CreateAccount;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Enums;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class CreateAccountHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateAccountHandler CreateHandler() => new(_accounts, _uow);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithCorrectCurrency()
    {
        var cmd = new CreateAccountCommand(Guid.NewGuid(), Currency.UAH);

        var result = await CreateHandler().HandleAsync(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Currency.UAH, result.Value.Currency);
        Assert.Equal(0m, result.Value.Balance);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAccountAndSavesChanges()
    {
        var cmd = new CreateAccountCommand(Guid.NewGuid(), Currency.USD);

        await CreateHandler().HandleAsync(cmd, CancellationToken.None);

        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Currency == Currency.USD && a.UserId == cmd.UserId),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}