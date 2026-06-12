using Microsoft.EntityFrameworkCore;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Infrastructure.Persistence.Repositories;

public class AccountRepository(AppDbContext db) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Accounts.FindAsync([id], ct);

    public async Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Account account, CancellationToken ct = default)
        => await db.Accounts.AddAsync(account, ct);
}