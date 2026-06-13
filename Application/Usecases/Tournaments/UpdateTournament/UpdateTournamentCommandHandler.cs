using MediatR;

namespace Application.Usecases.Tournaments.UpdateTournament;

public sealed class UpdateTournamentCommandHandler
    : IRequestHandler<UpdateTournamentCommand, bool>
{
    public Task<bool> Handle(
        UpdateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Tournament name is required.");
        }

        // TODO: Update tournament in database

        return Task.FromResult(true);
    }
}