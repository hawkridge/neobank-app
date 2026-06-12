using NeoBank.Application.DTOs;
using NeoBank.Application.Interfaces;
using NeoBank.Domain.Common;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Commands.CreateAccount;

public class CreateAccountHandler(IAccountRepository accounts, IUnitOfWork uow)
{
    public async Task<Result<AccountDto>> HandleAsync(CreateAccountCommand cmd, CancellationToken ct)
    {
        var existing = await accounts.GetByUserIdAsync(cmd.UserId, ct);
        if (existing.Any(a => a.Currency == cmd.Currency))
            return Result<AccountDto>.Failure("Account.DuplicateCurrency");

        var account = new Account(cmd.UserId, cmd.Currency);
        await accounts.AddAsync(account, ct);
        await uow.SaveChangesAsync(ct);

        return Result<AccountDto>.Success(account.ToDto());
    }
}