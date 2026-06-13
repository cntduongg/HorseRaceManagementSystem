using MediatR;

namespace Application.Usecases.Tournaments.CreateTournament;

public sealed class CreateTournamentCommandHandler
    : IRequestHandler<CreateTournamentCommand, int>
{
    public Task<int> Handle(
        CreateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Tournament name is required.");
        }

        if (request.StartDate > request.EndDate)
        {
            throw new InvalidOperationException(
                "StartDate cannot be later than EndDate.");
        }

        // TODO: Save tournament into database

        var tournamentId = Random.Shared.Next(1, int.MaxValue);

        return Task.FromResult(tournamentId);
    }
}