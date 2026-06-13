using MediatR;

namespace Application.Usecases.SettlementRuns.CreateSettlementRun;

public sealed record CreateSettlementRunCommand(
    int RaceId,
    string Type,
    string Status,
    int TotalPredictions,
    decimal TotalBetAmount,
    decimal TotalPayoutAmount,
    int? TriggeredByAdminId
) : IRequest<int>;