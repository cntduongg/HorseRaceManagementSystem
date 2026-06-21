using MediatR;

namespace Application.Usecases.SettlementRuns.GetSettlementRunDetail;

public sealed class GetSettlementRunDetailQueryHandler
    : IRequestHandler<GetSettlementRunDetailQuery, SettlementRunDetailResponse?>
{
    public Task<SettlementRunDetailResponse?> Handle(
        GetSettlementRunDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new SettlementRunDetailResponse(
            request.SettlementRunId,
            1,
            "Publish",
            "Completed",
            100,
            10000,
            15000,
            1
        );

        return Task.FromResult<SettlementRunDetailResponse?>(response);
    }
}