namespace Application.Usecases.Predictions.GetPredictionList;

public sealed record PredictionListItemResponse(
    int PredictionId,
    int RaceId,
    int SpectatorId,
    decimal BetAmount,
    string Status
);