using MediatR;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultList;

public sealed class GetLegOfficialResultListQueryHandler
    : IRequestHandler<
        GetLegOfficialResultListQuery,
        List<LegOfficialResultListItemResponse>>
{
    public Task<List<LegOfficialResultListItemResponse>> Handle(
        GetLegOfficialResultListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var result = new List<LegOfficialResultListItemResponse>
        {
            new(
                1,
                1,
                1,
                1,
                "Finished"
            )
        };

        return Task.FromResult(result);
    }
}