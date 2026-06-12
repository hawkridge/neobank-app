using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NeoBank.Domain.Common;
using NeoBank.Domain.Interfaces;

namespace NeoBank.Infrastructure.Persistence.Interceptors;

// Dispatches domain events after a successful SaveChangesAsync commit, so that
// side effects run only once the transaction has actually persisted.
public class DomainEventDispatcher(IServiceProvider rootProvider) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return result;

        var entitiesWithEvents = eventData.Context.ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(entity => entity.DomainEvents)
            .ToList();

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        // Resolve handlers in a fresh scope: the request scope is mid-SaveChanges.
        using var scope = rootProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                await ((dynamic)handler).HandleAsync((dynamic)domainEvent, cancellationToken);
            }
        }

        return result;
    }
}