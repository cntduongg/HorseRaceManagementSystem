using MediatR;

namespace Application.Usecases.Races.UpdateRace;

public sealed class UpdateRaceCommandHandler
    : IRequestHandler<UpdateRaceCommand, bool>
{
    public Task<bool> Handle(
        UpdateRaceCommand request,
        CancellationToken cancellationToken)
    {
        // TODO:
        // Find race
        // Update race
        // SaveChanges

        return Task.FromResult(true);
    }
}