using NeoBank.Domain.Common;
using NeoBank.Domain.Enums;

namespace NeoBank.Domain.Events;

public record AccountCreated(Guid AccountId, Guid UserId, Currency Currency) : IDomainEvent;