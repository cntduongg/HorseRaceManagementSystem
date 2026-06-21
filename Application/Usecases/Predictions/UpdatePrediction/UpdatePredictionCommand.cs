using MediatR;

namespace Application.Usecases.Predictions.UpdatePrediction;

public sealed record UpdatePredictionCommand(
    int PredictionId,
    int RaceId,
    int SpectatorId,
    int FirstEntryId,
    int SecondEntryId,
    int ThirdEntryId,
    decimal BetAmount,
    decimal OddsLocked1,
    decimal OddsLocked2,
    decimal OddsLocked3,
    string Status
) : IRequest<bool>;