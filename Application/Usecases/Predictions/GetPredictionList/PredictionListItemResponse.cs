namespace Application.Usecases.Predictions.GetPredictionList;

public sealed record PredictionListItemResponse(
    int PredictionId,
    int RaceId,
    int LegNumber,
    int SpectatorId,
    decimal BetAmount,
    string Status
);