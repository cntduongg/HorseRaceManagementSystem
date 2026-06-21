using MediatR;

namespace Application.Usecases.Predictions.GetPredictionDetail;

public sealed class GetPredictionDetailQueryHandler
    : IRequestHandler<GetPredictionDetailQuery, PredictionDetailResponse?>
{
    public Task<PredictionDetailResponse?> Handle(
        GetPredictionDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load prediction from database

        var response = new PredictionDetailResponse(
            PredictionId: request.PredictionId,
            RaceId: 1,
            SpectatorId: 1,
            FirstEntryId: 1,
            SecondEntryId: 2,
            ThirdEntryId: 3,
            BetAmount: 100,
            OddsLocked1: 1.5m,
            OddsLocked2: 2.0m,
            OddsLocked3: 2.5m,
            Status: "Pending"
        );

        return Task.FromResult<PredictionDetailResponse?>(response);
    }
}