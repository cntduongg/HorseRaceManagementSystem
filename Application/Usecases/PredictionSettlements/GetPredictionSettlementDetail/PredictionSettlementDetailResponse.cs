namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementDetail;

public sealed record PredictionSettlementDetailResponse(
    int PredictionSettlementId,
    int SettlementRunId,
    int PredictionId,
    int RaceId,
    int SpectatorId,
    int MatchedCount,
    string Outcome,
    decimal BetAmount,
    decimal OddsAverage,
    decimal PayoutAmount,
    decimal NetAmount,
    bool IsRollbacked
);