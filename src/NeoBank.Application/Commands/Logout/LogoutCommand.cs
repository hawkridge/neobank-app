namespace NeoBank.Application.Commands.Logout;

public record LogoutCommand(Guid UserId, string RefreshToken);