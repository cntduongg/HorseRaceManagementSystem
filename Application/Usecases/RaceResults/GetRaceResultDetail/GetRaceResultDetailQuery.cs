using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed record GetRaceResultDetailQuery(
	int RaceId,
	int EntryId
) : IRequest<RaceResultDetailResponse?>;