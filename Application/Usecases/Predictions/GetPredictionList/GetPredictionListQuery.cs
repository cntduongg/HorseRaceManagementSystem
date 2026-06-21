using MediatR;

namespace Application.Usecases.Predictions.GetPredictionList;

public sealed record GetPredictionListQuery()
    : IRequest<List<PredictionListItemResponse>>;