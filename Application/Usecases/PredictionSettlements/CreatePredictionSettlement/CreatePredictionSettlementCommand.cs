using MediatR;

namespace Application.Usecases.PredictionSettlements.CreatePredictionSettlement;

public sealed record CreatePredictionSettlementCommand(
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
    int? PayoutTransactionId
) : IRequest<int>;