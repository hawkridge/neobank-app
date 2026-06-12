using NeoBank.Domain.Enums;

namespace NeoBank.Application.Commands.CreateAccount;

public record CreateAccountCommand(Guid UserId, Currency Currency);