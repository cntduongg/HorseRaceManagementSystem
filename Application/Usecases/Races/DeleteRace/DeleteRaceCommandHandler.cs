using Application.Usecases.RaceExecution;
using MediatR;

namespace Application.Usecases.Races.DeleteRace;

public sealed class DeleteRaceCommandHandler
    : IRequestHandler<DeleteRaceCommand, bool>
{
    private readonly IRaceLifecycleCoordinator _lifecycle;

    public DeleteRaceCommandHandler(IRaceLifecycleCoordinator lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public async Task<bool> Handle(
        DeleteRaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.CancelRaceAsync(
                request.RaceId,
                reason: "Race cancelled by admin.",
                throwOnFailure: true,
                cancellationToken);

            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}