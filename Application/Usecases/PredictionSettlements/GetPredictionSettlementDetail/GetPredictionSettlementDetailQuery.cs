using MediatR;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementDetail;

public sealed record GetPredictionSettlementDetailQuery(
    int PredictionSettlementId
) : IRequest<PredictionSettlementDetailResponse?>;