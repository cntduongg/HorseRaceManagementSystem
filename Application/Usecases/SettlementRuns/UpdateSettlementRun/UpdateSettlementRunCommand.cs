using MediatR;

namespace Application.Usecases.SettlementRuns.UpdateSettlementRun;

public sealed record UpdateSettlementRunCommand(
    int SettlementRunId,
    int RaceId,
    string Type,
    string Status,
    int TotalPredictions,
    decimal TotalBetAmount,
    decimal TotalPayoutAmount,
    int? TriggeredByAdminId
) : IRequest<bool>;