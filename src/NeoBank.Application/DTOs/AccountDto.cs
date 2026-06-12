using NeoBank.Domain.Enums;

namespace NeoBank.Application.DTOs;

public record AccountDto(
    Guid Id,
    string AccountNumber,
    Currency Currency,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt
);