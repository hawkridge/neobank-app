using Microsoft.Extensions.Options;
using NeoBank.Application.Commands.Refresh;
using NeoBank.Application.Configuration;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class RefreshHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IOptions<JwtSettings> _jwt =
        Options.Create(new JwtSettings { AccessTokenExpiryMinutes = 15, RefreshTokenExpiryDays = 7 });

    private RefreshHandler CreateHandler() => new(_users, _tokens, _uow, _jwt);

    private static readonly Guid UserId = Guid.NewGuid();

    private static RefreshToken ActiveToken() =>
        new(UserId, "old_refresh", DateTime.UtcNow.AddDays(7));

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokenPair()
    {
        _users.GetRefreshTokenAsync("old_refresh", Arg.Any<CancellationToken>()).Returns(ActiveToken());
        _tokens.GenerateAccessToken(Arg.Any<User>()).Returns("new_access");
        _tokens.GenerateRefreshToken().Returns("new_refresh");

        var result = await CreateHandler().HandleAsync(
            new RefreshCommand("old_refresh"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new_access", result.Value.AccessToken);
        Assert.Equal("new_refresh", result.Value.RefreshToken);
        Assert.Equal(15 * 60, result.Value.ExpiresIn);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesOldForNew()
    {
        var stored = ActiveToken();
        _users.GetRefreshTokenAsync("old_refresh", Arg.Any<CancellationToken>()).Returns(stored);
        _tokens.GenerateRefreshToken().Returns("new_refresh");

        await CreateHandler().HandleAsync(new RefreshCommand("old_refresh"), CancellationToken.None);

        Assert.True(stored.IsRevoked);
        await _users.Received(1).AddRefreshTokenAsync(
            Arg.Is<RefreshToken>(t => t.Token == "new_refresh" && t.UserId == UserId),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsInvalidAndPersistsNothing()
    {
        _users.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var result = await CreateHandler().HandleAsync(
            new RefreshCommand("garbage"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error);
        await _users.DidNotReceive().AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevokedToken_DetectsReuseAndRevokesWholeFamily()
    {
        var revoked = ActiveToken();
        revoked.Revoke(); // already rotated away — presenting it again is suspicious
        _users.GetRefreshTokenAsync("old_refresh", Arg.Any<CancellationToken>()).Returns(revoked);

        var result = await CreateHandler().HandleAsync(
            new RefreshCommand("old_refresh"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RefreshTokenReuseDetected", result.Error);
        await _users.Received(1).RevokeAllRefreshTokensForUserAsync(UserId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _users.DidNotReceive().AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsExpiredAndPersistsNothing()
    {
        var expired = new RefreshToken(UserId, "old_refresh", DateTime.UtcNow.AddDays(-1));
        _users.GetRefreshTokenAsync("old_refresh", Arg.Any<CancellationToken>()).Returns(expired);

        var result = await CreateHandler().HandleAsync(
            new RefreshCommand("old_refresh"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.RefreshTokenExpired", result.Error);
        await _users.DidNotReceive().AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}