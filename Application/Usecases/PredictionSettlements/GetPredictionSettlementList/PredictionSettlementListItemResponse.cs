namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementList;

public sealed record PredictionSettlementListItemResponse(
    int PredictionSettlementId,
    int PredictionId,
    string Outcome,
    decimal PayoutAmount,
    bool IsRollbacked
);