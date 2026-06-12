using NeoBank.Application.DTOs;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Common;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Commands.Register;

public class RegisterHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork uow)
{
    public async Task<Result<RegisterResponseDto>> HandleAsync(RegisterCommand cmd, CancellationToken ct)
    {
        if (await users.ExistsByEmailAsync(cmd.Email, ct))
            return Result<RegisterResponseDto>.Failure("Auth.EmailTaken");

        var hash = hasher.Hash(cmd.Password);
        var user = new User(cmd.Email, hash, cmd.FirstName, cmd.LastName, cmd.PhoneNumber);

        await users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        return Result<RegisterResponseDto>.Success(
            new RegisterResponseDto(user.Id, user.Email, user.FirstName, user.LastName));
    }
}