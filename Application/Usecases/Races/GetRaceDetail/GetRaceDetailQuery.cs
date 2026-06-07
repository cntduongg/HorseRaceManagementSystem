using MediatR;

namespace Application.Usecases.Races.GetRaceDetail;

public sealed record GetRaceDetailQuery(Guid RaceId)
    : IRequest<RaceDetailResponse?>;