using Microsoft.EntityFrameworkCore;
using NeoBank.Domain.Entities;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default) =>
        db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);
    
    public async Task RevokeAllRefreshTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);
          
        foreach (var token in active)
            token.Revoke();
    }   
}