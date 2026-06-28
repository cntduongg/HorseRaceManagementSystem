namespace Application.Usecases.Predictions.PlacePrediction;

public sealed record PlacePredictionResponse(
    int PredictionId,
    int RaceId,
    int SpectatorId,
    int EntryId,
    decimal BetAmount,
    decimal OddsLocked,
    string Status,
    DateTime CreatedAt);