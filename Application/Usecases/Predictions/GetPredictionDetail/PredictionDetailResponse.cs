namespace Application.Usecases.Predictions.GetPredictionDetail;

public sealed record PredictionDetailResponse(
    int PredictionId,
    int RaceId,
    int LegNumber,
    int SpectatorId,
    int FirstEntryId,
    int? SecondEntryId,
    int? ThirdEntryId,
    decimal BetAmount,
    decimal OddsLocked1,
    decimal? OddsLocked2,
    decimal? OddsLocked3,
    string Status);