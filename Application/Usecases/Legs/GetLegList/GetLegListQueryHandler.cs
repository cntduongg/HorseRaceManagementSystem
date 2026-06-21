using MediatR;

namespace Application.Usecases.Legs.GetLegList;

public sealed class GetLegListQueryHandler
    : IRequestHandler<GetLegListQuery, List<LegListItemResponse>>
{
    public Task<List<LegListItemResponse>> Handle(
        GetLegListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var result = new List<LegListItemResponse>
        {
            new(1, 1, "Pending"),
            new(1, 2, "Confirmed")
        };

        return Task.FromResult(result);
    }
}