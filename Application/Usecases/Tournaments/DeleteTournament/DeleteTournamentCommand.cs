using MediatR;

namespace Application.Usecases.Tournaments.DeleteTournament;

public sealed record DeleteTournamentCommand(
    int TournamentId
) : IRequest<bool>;