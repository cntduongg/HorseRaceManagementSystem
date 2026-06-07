namespace ShareKernel.Aggregate;

public interface IDomainEventPublisher
{
    Task PublishAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default, int maxConcurrency = 3);
    Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default);
}