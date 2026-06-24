using Domain.Aggregates.Entities;

namespace Application.Common;

public interface IHorseRepository
{
	Task<Horse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

	Task<List<Horse>> GetPendingAsync(CancellationToken cancellationToken = default);

	Task UpdateAsync(Horse horse, CancellationToken cancellationToken = default);
}