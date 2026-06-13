using MediatR;

namespace Application.Usecases.SettlementRuns.DeleteSettlementRun;

public sealed record DeleteSettlementRunCommand(
    int SettlementRunId
) : IRequest<bool>;