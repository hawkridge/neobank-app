using NeoBank.Domain.Entities;

namespace NeoBank.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
}
