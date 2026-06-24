using Application.Usecases.Tournaments.GetTournamentList;

namespace Application.Common;

public interface ITournamentReadService
{
    Task<List<TournamentListItemResponse>> GetListAsync(
        CancellationToken cancellationToken);
}