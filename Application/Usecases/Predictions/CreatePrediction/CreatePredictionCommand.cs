using MediatR;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed record CreatePredictionCommand(
    int RaceId,
    int LegNumber,
    int SpectatorId,
    int FirstEntryId,
    decimal BetAmount
) : IRequest<int>;