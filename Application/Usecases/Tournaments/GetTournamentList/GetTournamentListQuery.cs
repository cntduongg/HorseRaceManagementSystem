using MediatR;

namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed record GetTournamentListQuery()
    : IRequest<List<TournamentListItemResponse>>;