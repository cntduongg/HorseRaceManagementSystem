using Domain.Aggregates.Entities;

namespace Application.Common;

public interface IEntryRepository
{
    Task<List<Entry>> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(IEnumerable<Entry> entries, CancellationToken cancellationToken = default);
}