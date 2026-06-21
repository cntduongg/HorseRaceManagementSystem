using MediatR;

namespace Application.Usecases.Races.DeleteRace;

public sealed class DeleteRaceCommandHandler
    : IRequestHandler<DeleteRaceCommand, bool>
{
    public Task<bool> Handle(
        DeleteRaceCommand request,
        CancellationToken cancellationToken)
    {
        // TODO:
        // Find race
        // Delete race
        // SaveChanges

        return Task.FromResult(true);
    }
}