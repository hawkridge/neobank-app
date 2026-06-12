using NeoBank.Application.Commands.Register;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;
using NSubstitute;

namespace NeoBank.Application.Tests;

public class RegisterHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RegisterHandler CreateHandler() => new(_users, _hasher, _uow);

    private static RegisterCommand ValidCommand() => new(
        Email: "john@example.com",
        Password: "Secret123",
        FirstName: "John",
        LastName: "Doe",
        PhoneNumber: "+380991234567");

    [Fact]
    public async Task Handle_NewEmail_ReturnsSuccessWithUserData()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var result = await CreateHandler().HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("john@example.com", result.Value.Email);
        Assert.Equal("John", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
    }

    [Fact]
    public async Task Handle_NewEmail_HashesPasswordBeforePersisting()
    {
        _hasher.Hash("Secret123").Returns("bcrypt_hash");

        await CreateHandler().HandleAsync(ValidCommand(), CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash == "bcrypt_hash"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewEmail_SavesChanges()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        await CreateHandler().HandleAsync(ValidCommand(), CancellationToken.None);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailureWithoutPersisting()
    {
        _users.ExistsByEmailAsync("john@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailTaken", result.Error);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}