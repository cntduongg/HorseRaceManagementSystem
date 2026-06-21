using MediatR;

namespace Application.Usecases.Predictions.DeletePrediction;

public sealed record DeletePredictionCommand(
    int PredictionId
) : IRequest<bool>;