using Application.Common;
using MediatR;

namespace Application.Usecases.Races.GetRaceList;

public sealed class GetRaceListQueryHandler
    : IRequestHandler<GetRaceListQuery, List<RaceListItemResponse>>
{
    private readonly IRaceReadService _raceReadService;

    public GetRaceListQueryHandler(IRaceReadService raceReadService)
    {
        _raceReadService = raceReadService;
    }

    public Task<List<RaceListItemResponse>> Handle(
        GetRaceListQuery request,
        CancellationToken cancellationToken)
    {
        return _raceReadService.GetListAsync(cancellationToken);
    }
}