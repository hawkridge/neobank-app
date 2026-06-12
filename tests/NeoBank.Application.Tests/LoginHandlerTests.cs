using Microsoft.Extensions.Options;
using NeoBank.Application.Commands.Login;
using NeoBank.Application.Configuration;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class LoginHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IOptions<JwtSettings> _jwt =
        Options.Create(new JwtSettings { AccessTokenExpiryMinutes = 15, RefreshTokenExpiryDays = 7 });

    private LoginHandler CreateHandler() => new(_users, _hasher, _tokens, _uow, _jwt);

    private static User ExistingUser() => new(
        email: "john@example.com",
        passwordHash: "stored_hash",
        firstName: "John",
        lastName: "Doe",
        phoneNumber: "+380991234567");

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var user = ExistingUser();
        _users.GetByEmailAsync("john@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("Secret123", "stored_hash").Returns(true);
        _tokens.GenerateAccessToken(user).Returns("access_token");
        _tokens.GenerateRefreshToken().Returns("refresh_token");

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("john@example.com", "Secret123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access_token", result.Value.AccessToken);
        Assert.Equal("refresh_token", result.Value.RefreshToken);
        Assert.Equal(15 * 60, result.Value.ExpiresIn);
    }

    [Fact]
    public async Task Handle_ValidCredentials_PersistsRefreshToken()
    {
        var user = ExistingUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokens.GenerateRefreshToken().Returns("refresh_token");

        await CreateHandler().HandleAsync(
            new LoginCommand("john@example.com", "Secret123"), CancellationToken.None);

        await _users.Received(1).AddRefreshTokenAsync(
            Arg.Is<RefreshToken>(t => t.Token == "refresh_token" && t.UserId == user.Id),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("nobody@example.com", "Secret123"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ExistingUser());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CreateHandler().HandleAsync(
            new LoginCommand("john@example.com", "WrongPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error);
    }

    [Fact]
    public async Task Handle_WrongPassword_SameErrorAsUnknownEmail()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var notFoundResult = await CreateHandler().HandleAsync(
            new LoginCommand("nobody@example.com", "Secret123"), CancellationToken.None);

        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ExistingUser());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var wrongPassResult = await CreateHandler().HandleAsync(
            new LoginCommand("john@example.com", "WrongPass1"), CancellationToken.None);

        Assert.Equal(notFoundResult.Error, wrongPassResult.Error);
    }

    [Fact]
    public async Task Handle_FailedLogin_DoesNotPersistAnything()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await CreateHandler().HandleAsync(
            new LoginCommand("nobody@example.com", "Secret123"), CancellationToken.None);

        await _users.DidNotReceive().AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}