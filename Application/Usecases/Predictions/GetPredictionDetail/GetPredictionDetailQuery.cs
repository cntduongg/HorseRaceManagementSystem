using MediatR;

namespace Application.Usecases.Predictions.GetPredictionDetail;

public sealed record GetPredictionDetailQuery(
    int PredictionId
) : IRequest<PredictionDetailResponse?>;