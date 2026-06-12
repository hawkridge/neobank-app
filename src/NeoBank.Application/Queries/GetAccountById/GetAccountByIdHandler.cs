using NeoBank.Application.DTOs;
using NeoBank.Domain.Common;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Queries.GetAccountById;

public class GetAccountByIdHandler(IAccountRepository accounts)
{
    public async Task<Result<AccountDto>> HandleAsync(GetAccountByIdQuery query, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(query.AccountId, ct);

        if (account is null)
            return Result<AccountDto>.Failure("Account.NotFound");

        if (account.UserId != query.UserId)
            return Result<AccountDto>.Failure("Account.AccessDenied");

        return Result<AccountDto>.Success(account.ToDto());
    }
}