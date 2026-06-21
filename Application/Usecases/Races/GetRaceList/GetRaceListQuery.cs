using MediatR;

namespace Application.Usecases.Races.GetRaceList;

public sealed record GetRaceListQuery()
    : IRequest<List<RaceListItemResponse>>;