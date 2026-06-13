using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed record GetRaceResultListQuery()
    : IRequest<List<RaceResultListItemResponse>>;