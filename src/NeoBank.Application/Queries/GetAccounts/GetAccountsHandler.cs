using NeoBank.Application.DTOs;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.Queries.GetAccounts;

public class GetAccountsHandler(IAccountRepository accounts)
{
    public async Task<IReadOnlyList<AccountDto>> HandleAsync(GetAccountsQuery query, CancellationToken ct)
    {
        var userAccounts = await accounts.GetByUserIdAsync(query.UserId, ct);
        return userAccounts.Select(a => a.ToDto()).ToList();
    }
}