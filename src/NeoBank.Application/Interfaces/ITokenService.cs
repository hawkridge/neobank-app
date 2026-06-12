using NeoBank.Domain.Entities;

namespace NeoBank.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}