using Application.Usecases.RaceResults.GetRaceResultDetail;
using Application.Usecases.RaceResults.GetRaceResultList;

namespace Application.Common;

public interface IRaceResultReadService
{
    Task<List<RaceResultListItemResponse>> GetListAsync(
        CancellationToken cancellationToken);

    Task<RaceResultDetailResponse?> GetDetailAsync(
        int raceId,
        int entryId,
        CancellationToken cancellationToken);
}