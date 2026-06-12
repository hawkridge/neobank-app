using Microsoft.Extensions.Logging;
using NeoBank.Domain.Events;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Application.EventHandlers;

public class AccountCreatedLogger(ILogger<AccountCreatedLogger> logger)
    : IDomainEventHandler<AccountCreated>
{
    public Task HandleAsync(AccountCreated domainEvent, CancellationToken ct)
    {
        logger.LogInformation(
            "Domain event: Account {AccountId} created for user {UserId} in {Currency}",
            domainEvent.AccountId, domainEvent.UserId, domainEvent.Currency);
        return Task.CompletedTask;
    }
}