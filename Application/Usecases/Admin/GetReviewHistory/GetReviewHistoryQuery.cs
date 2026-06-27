using Application.Common;
using Domain.Aggregates.Enums;

namespace Application.Usecases.Admin.GetReviewHistory;

public sealed record GetReviewHistoryQuery(
    ReviewEntity? Entity,
    int? EntityId
) : IQuery<List<ReviewHistoryResponse>>;