using MediatR;

namespace Application.Usecases.Entries.CreateEntry;

public sealed class CreateEntryCommandHandler
    : IRequestHandler<CreateEntryCommand, int>
{
    public Task<int> Handle(
        CreateEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
        {
            throw new InvalidOperationException("RaceId is required.");
        }

        if (request.HorseId <= 0)
        {
            throw new InvalidOperationException("HorseId is required.");
        }

        if (request.JockeyId <= 0)
        {
            throw new InvalidOperationException("JockeyId is required.");
        }

        if (request.HorseOwnerId <= 0)
        {
            throw new InvalidOperationException("HorseOwnerId is required.");
        }

        // TODO: Save entry into database

        var entryId = 1;

        return Task.FromResult(entryId);
    }
}