using MediatR;

namespace Application.Usecases.Violations.GetViolationList;

public sealed class GetViolationListQueryHandler
    : IRequestHandler<
        GetViolationListQuery,
        List<ViolationListItemResponse>>
{
    public Task<List<ViolationListItemResponse>> Handle(
        GetViolationListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var result = new List<ViolationListItemResponse>
        {
            new(
                1,
                1,
                1,
                1,
                "LaneViolation",
                "Warning",
                "Pending"
            )
        };

        return Task.FromResult(result);
    }
}