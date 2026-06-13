using MediatR;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultDetail;

public sealed record GetLegOfficialResultDetailQuery(
    int RaceId,
    int LegNumber,
    int EntryId
) : IRequest<LegOfficialResultDetailResponse?>;