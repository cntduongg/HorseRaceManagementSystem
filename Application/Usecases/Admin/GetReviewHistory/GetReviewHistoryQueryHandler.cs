using Application.Common;
using MediatR;

namespace Application.Usecases.Admin.GetReviewHistory;

public sealed class GetReviewHistoryQueryHandler
    : IRequestHandler<GetReviewHistoryQuery, List<ReviewHistoryResponse>>
{
    private readonly IReviewHistoryRepository _repository;

    public GetReviewHistoryQueryHandler(
        IReviewHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ReviewHistoryResponse>> Handle(
        GetReviewHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var histories = await _repository.GetAsync(
            request.Entity,
            request.EntityId,
            cancellationToken);

        return histories
            .Select(x => new ReviewHistoryResponse(
                x.Id,
                x.EntityType.ToString(),
                x.EntityId,
                x.Action.ToString(),
                x.Reason,
                x.AdminId,
                x.Admin.FullName,
                x.CreatedAt))
            .ToList();
    }
}