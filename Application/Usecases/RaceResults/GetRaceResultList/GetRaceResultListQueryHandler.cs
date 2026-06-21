using Application.Common;
using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed class GetRaceResultListQueryHandler
    : IRequestHandler<GetRaceResultListQuery, List<RaceResultListItemResponse>>
{
    private readonly IRaceResultReadService _raceResultReadService;

    public GetRaceResultListQueryHandler(IRaceResultReadService raceResultReadService)
    {
        _raceResultReadService = raceResultReadService;
    }

    public Task<List<RaceResultListItemResponse>> Handle(
        GetRaceResultListQuery request,
        CancellationToken cancellationToken)
    {
        return _raceResultReadService.GetListAsync(cancellationToken);
    }
}