using NeoBank.Domain.Entities;

namespace NeoBank.Application.DTOs;

public static class AccountMappingExtensions
{
    public static AccountDto ToDto(this Account account) => new(
        account.Id,
        account.AccountNumber,
        account.Currency,
        account.Balance,
        account.IsActive,
        account.CreatedAt
    );
}