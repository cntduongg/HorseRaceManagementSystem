using Domain.Aggregates.Entities;

namespace Application.Common;

public interface IHorseRepository
{
    /// <summary>Loads a tracked horse for mutation (approve/reject/revoke/update).</summary>
    Task<Horse?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default);

    /// <summary>Read-only listing, optionally filtered by owner and/or status.</summary>
    Task<List<Horse>> GetAsync(int? ownerId, string? status, CancellationToken cancellationToken = default);

    Task AddAsync(Horse horse, CancellationToken cancellationToken = default);

    void Remove(Horse horse);
}
