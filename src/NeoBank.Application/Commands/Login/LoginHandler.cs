using Microsoft.Extensions.Options;
using NeoBank.Application.Configuration;
using NeoBank.Application.DTOs;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Common;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Commands.Login;

public class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    IUnitOfWork uow,
    IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<Result<LoginResponseDto>> HandleAsync(LoginCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email, ct);

        // Deliberate: same error for "not found" and "wrong password" to prevent user enumeration.
        if (user is null || !hasher.Verify(cmd.Password, user.PasswordHash))
            return Result<LoginResponseDto>.Failure("Auth.InvalidCredentials");

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshTokenValue = tokens.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            user.Id, refreshTokenValue, DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays));
        await users.AddRefreshTokenAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return Result<LoginResponseDto>.Success(
            new LoginResponseDto(accessToken, refreshTokenValue, _jwt.AccessTokenExpiryMinutes * 60));
    }
}