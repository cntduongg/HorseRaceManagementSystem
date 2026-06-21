using MediatR;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed record CreatePredictionCommand(
    int RaceId,
    int SpectatorId,
    int FirstEntryId,
    int SecondEntryId,
    int ThirdEntryId,
    decimal BetAmount,
    decimal OddsLocked1,
    decimal OddsLocked2,
    decimal OddsLocked3
) : IRequest<int>;