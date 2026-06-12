namespace NeoBank.Application.DTOs;

public record RegisterResponseDto(Guid UserId, string Email, string FirstName, string LastName);

public record LoginResponseDto(string AccessToken, string RefreshToken, int ExpiresIn);