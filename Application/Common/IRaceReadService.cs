using Application.Usecases.Races.GetRaceList;

namespace Application.Common;

public interface IRaceReadService
{
    Task<List<RaceListItemResponse>> GetListAsync(
        CancellationToken cancellationToken);
}