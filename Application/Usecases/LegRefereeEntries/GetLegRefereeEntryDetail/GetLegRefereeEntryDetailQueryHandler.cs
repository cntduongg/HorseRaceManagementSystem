using MediatR;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryDetail;

public sealed class GetLegRefereeEntryDetailQueryHandler
    : IRequestHandler<
        GetLegRefereeEntryDetailQuery,
        LegRefereeEntryDetailResponse?>
{
    public Task<LegRefereeEntryDetailResponse?> Handle(
        GetLegRefereeEntryDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new LegRefereeEntryDetailResponse(
            request.LegRefereeEntryId,
            1,
            1,
            1,
            2,
            1,
            "Finished",
            DateTime.UtcNow
        );

        return Task.FromResult<LegRefereeEntryDetailResponse?>(response);
    }
}