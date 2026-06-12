using NeoBank.Domain.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace NeoBank.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    // Work factor 12 = ~250ms per hash. Enough to slow brute-force, not noticeable for users.
    private const int WorkFactor = 12;

    public string Hash(string password) => BC.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BC.Verify(password, hash);
}