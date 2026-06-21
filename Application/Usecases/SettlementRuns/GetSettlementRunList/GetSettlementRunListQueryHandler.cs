using MediatR;

namespace Application.Usecases.SettlementRuns.GetSettlementRunList;

public sealed class GetSettlementRunListQueryHandler
    : IRequestHandler<GetSettlementRunListQuery, List<SettlementRunListItemResponse>>
{
    public Task<List<SettlementRunListItemResponse>> Handle(
        GetSettlementRunListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var runs = new List<SettlementRunListItemResponse>
        {
            new(
                1,
                1,
                "Publish",
                "Completed"
            ),
            new(
                2,
                2,
                "Rollback",
                "Completed"
            )
        };

        return Task.FromResult(runs);
    }
}