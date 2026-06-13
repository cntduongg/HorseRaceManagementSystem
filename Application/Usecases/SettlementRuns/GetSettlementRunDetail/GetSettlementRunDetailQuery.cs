using MediatR;

namespace Application.Usecases.SettlementRuns.GetSettlementRunDetail;

public sealed record GetSettlementRunDetailQuery(
    int SettlementRunId
) : IRequest<SettlementRunDetailResponse?>;