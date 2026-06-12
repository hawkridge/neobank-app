using NeoBank.Application.Commands.Logout;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class LogoutHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private LogoutHandler CreateHandler() => new(_users, _uow);

    private static readonly Guid UserId = Guid.NewGuid();

    private static RefreshToken TokenFor(Guid ownerId) =>
        new(ownerId, "refresh", DateTime.UtcNow.AddDays(7));

    [Fact]
    public async Task Handle_OwnedToken_RevokesAndSaves()
    {
        var token = TokenFor(UserId);
        _users.GetRefreshTokenAsync("refresh", Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().HandleAsync(
            new LogoutCommand(UserId, "refresh"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(token.IsRevoked);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_SucceedsWithoutSaving()
    {
        _users.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var result = await CreateHandler().HandleAsync(
            new LogoutCommand(UserId, "garbage"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenOwnedByAnotherUser_DoesNotRevoke()
    {
        var someoneElsesToken = TokenFor(Guid.NewGuid());
        _users.GetRefreshTokenAsync("refresh", Arg.Any<CancellationToken>()).Returns(someoneElsesToken);

        var result = await CreateHandler().HandleAsync(
            new LogoutCommand(UserId, "refresh"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(someoneElsesToken.IsRevoked);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}