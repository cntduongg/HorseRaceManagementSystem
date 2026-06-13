using MediatR;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementDetail;

public sealed class GetPredictionSettlementDetailQueryHandler
    : IRequestHandler<GetPredictionSettlementDetailQuery, PredictionSettlementDetailResponse?>
{
    public Task<PredictionSettlementDetailResponse?> Handle(
        GetPredictionSettlementDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new PredictionSettlementDetailResponse(
            request.PredictionSettlementId,
            1,
            1,
            1,
            1,
            2,
            "Won",
            100,
            2.5m,
            250,
            150,
            false
        );

        return Task.FromResult<PredictionSettlementDetailResponse?>(response);
    }
}