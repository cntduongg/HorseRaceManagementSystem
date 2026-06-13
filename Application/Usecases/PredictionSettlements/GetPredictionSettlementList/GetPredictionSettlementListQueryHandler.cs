using MediatR;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementList;

public sealed class GetPredictionSettlementListQueryHandler
    : IRequestHandler<GetPredictionSettlementListQuery, List<PredictionSettlementListItemResponse>>
{
    public Task<List<PredictionSettlementListItemResponse>> Handle(
        GetPredictionSettlementListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var items = new List<PredictionSettlementListItemResponse>
        {
            new(
                1,
                1,
                "Won",
                250,
                false
            ),
            new(
                2,
                2,
                "Lost",
                0,
                false
            )
        };

        return Task.FromResult(items);
    }
}