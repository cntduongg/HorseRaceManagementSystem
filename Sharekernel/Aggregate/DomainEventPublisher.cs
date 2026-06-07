using Microsoft.Extensions.DependencyInjection;
using ShareKernel.Aggregate;
namespace Sharekernel.Aggregate;

public class DomainEventPublisher(IServiceProvider serviceProvider) : IDomainEventPublisher
{
    public async Task PublishAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default, int maxConcurrency = 3)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = events.Select(async @event =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await PublishAsync(@event, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }

    public async Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());
        var handlers = serviceProvider.GetServices(handlerType);
        
        foreach (var handler in handlers)
        {
            await ((dynamic)handler!).Handle((dynamic)@event, cancellationToken);
        }
    }
}