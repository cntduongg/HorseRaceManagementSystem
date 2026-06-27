using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;

namespace Application.Common;

public interface IReviewHistoryRepository
{
    Task AddAsync(
        ReviewHistory history,
        CancellationToken cancellationToken = default);

    Task<List<ReviewHistory>> GetAsync(
        ReviewEntity? entity,
        int? entityId,
        CancellationToken cancellationToken = default);
}