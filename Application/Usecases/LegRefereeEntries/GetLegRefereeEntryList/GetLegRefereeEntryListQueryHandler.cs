using MediatR;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryList;

public sealed class GetLegRefereeEntryListQueryHandler
    : IRequestHandler<
        GetLegRefereeEntryListQuery,
        List<LegRefereeEntryListItemResponse>>
{
    public Task<List<LegRefereeEntryListItemResponse>> Handle(
        GetLegRefereeEntryListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var result = new List<LegRefereeEntryListItemResponse>
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