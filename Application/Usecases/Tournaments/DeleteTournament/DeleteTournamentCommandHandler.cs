using MediatR;

namespace Application.Usecases.Tournaments.DeleteTournament;

public sealed class DeleteTournamentCommandHandler
    : IRequestHandler<DeleteTournamentCommand, bool>
{
    public Task<bool> Handle(
        DeleteTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
        {
            return Task.FromResult(false);
        }

        // TODO: Delete tournament from database

        return Task.FromResult(true);
    }
}