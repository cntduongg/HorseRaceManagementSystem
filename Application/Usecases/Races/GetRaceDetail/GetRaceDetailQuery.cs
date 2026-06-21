using MediatR;

namespace Application.Usecases.Races.GetRaceDetail;

public sealed record GetRaceDetailQuery(int RaceId)
    : IRequest<RaceDetailResponse?>;