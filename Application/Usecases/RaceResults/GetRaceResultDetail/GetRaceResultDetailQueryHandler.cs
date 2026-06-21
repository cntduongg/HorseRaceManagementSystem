using Application.Common;
using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed class GetRaceResultDetailQueryHandler
    : IRequestHandler<GetRaceResultDetailQuery, RaceResultDetailResponse?>
{
    private readonly IRaceResultReadService _raceResultReadService;

    public GetRaceResultDetailQueryHandler(IRaceResultReadService raceResultReadService)
    {
        _raceResultReadService = raceResultReadService;
    }

    public Task<RaceResultDetailResponse?> Handle(
        GetRaceResultDetailQuery request,
        CancellationToken cancellationToken)
    {
        return _raceResultReadService.GetDetailAsync(
            request.RaceId,
            request.EntryId,
            cancellationToken);
    }
}