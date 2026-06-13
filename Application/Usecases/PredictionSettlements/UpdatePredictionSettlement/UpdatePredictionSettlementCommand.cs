using MediatR;

namespace Application.Usecases.PredictionSettlements.UpdatePredictionSettlement;

public sealed record UpdatePredictionSettlementCommand(
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
) : IRequest<bool>;