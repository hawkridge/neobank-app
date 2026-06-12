using Microsoft.Extensions.Options;
using NeoBank.Application.Configuration;
using NeoBank.Application.DTOs;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Common;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Commands.Refresh;

public class RefreshHandler(
    IUserRepository users,
    ITokenService tokens,
    IUnitOfWork uow,
    IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<Result<LoginResponseDto>> HandleAsync(RefreshCommand cmd, CancellationToken ct)
    {
        var stored = await users.GetRefreshTokenAsync(cmd.RefreshToken, ct);

        if (stored is null)
            return Result<LoginResponseDto>.Failure("Auth.InvalidRefreshToken");

        // A revoked token presented again implies theft: revoke the whole token family.
        if (stored.IsRevoked)
        {
            await users.RevokeAllRefreshTokensForUserAsync(stored.UserId, ct);
            await uow.SaveChangesAsync(ct);
            return Result<LoginResponseDto>.Failure("Auth.RefreshTokenReuseDetected");
        }

        if (stored.IsExpired)
            return Result<LoginResponseDto>.Failure("Auth.RefreshTokenExpired");

        // Rotate: revoke the presented token and issue a replacement in one commit.
        stored.Revoke();

        var newAccess = tokens.GenerateAccessToken(stored.User);
        var newRefreshValue = tokens.GenerateRefreshToken();
        var newRefresh = new RefreshToken(
            stored.UserId, newRefreshValue, DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays));
        await users.AddRefreshTokenAsync(newRefresh, ct);

        await uow.SaveChangesAsync(ct);

        return Result<LoginResponseDto>.Success(
            new LoginResponseDto(newAccess, newRefreshValue, _jwt.AccessTokenExpiryMinutes * 60));
    }
}