using MediatR;

namespace Application.Usecases.Predictions.GetPredictionList;

public sealed class GetPredictionListQueryHandler
    : IRequestHandler<GetPredictionListQuery,
        List<PredictionListItemResponse>>
{
    public Task<List<PredictionListItemResponse>> Handle(
        GetPredictionListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load predictions from database

        var predictions = new List<PredictionListItemResponse>
        {
            new(
                1,
                1,
                1,
                100,
                "Pending"
            ),
            new(
                2,
                1,
                2,
                200,
                "Pending"
            )
        };

        return Task.FromResult(predictions);
    }
}