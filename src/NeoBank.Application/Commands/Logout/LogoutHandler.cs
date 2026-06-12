using NeoBank.Application.Interfaces;
using NeoBank.Domain.Common;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Commands.Logout;

public class LogoutHandler(IUserRepository users, IUnitOfWork uow)
{
    public async Task<Result> HandleAsync(LogoutCommand cmd, CancellationToken ct)
    {
        var stored = await users.GetRefreshTokenAsync(cmd.RefreshToken, ct);

        // Idempotent: an unknown or foreign token is treated as already logged out.
        if (stored is null || stored.UserId != cmd.UserId)
            return Result.Success();

        stored.Revoke();
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}