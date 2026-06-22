using Application.Common;
using MediatR;

namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed class GetTournamentListQueryHandler
    : IRequestHandler<GetTournamentListQuery, List<TournamentListItemResponse>>
{
    private readonly ITournamentReadService _tournamentReadService;

    public GetTournamentListQueryHandler(ITournamentReadService tournamentReadService)
    {
        _tournamentReadService = tournamentReadService;
    }

    public Task<List<TournamentListItemResponse>> Handle(
        GetTournamentListQuery request,
        CancellationToken cancellationToken)
    {
        return _tournamentReadService.GetListAsync(cancellationToken);
    }
}