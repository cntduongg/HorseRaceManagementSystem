using MediatR;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryDetail;

public sealed record GetLegRefereeEntryDetailQuery(
    long LegRefereeEntryId
) : IRequest<LegRefereeEntryDetailResponse?>;