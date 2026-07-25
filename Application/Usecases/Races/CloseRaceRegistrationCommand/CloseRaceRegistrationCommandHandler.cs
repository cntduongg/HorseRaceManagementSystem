using Application.Common;
using Application.Usecases.RaceExecution;
using MediatR;

namespace Application.Usecases.Races.CloseRegistration;

public sealed class CloseRaceRegistrationCommandHandler
    : IRequestHandler<CloseRaceRegistrationCommand, bool>
{
    private readonly IRaceLifecycleCoordinator _lifecycle;

    public CloseRaceRegistrationCommandHandler(IRaceLifecycleCoordinator lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public async Task<bool> Handle(
        CloseRaceRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        // Cùng logic canonical với POST .../close-registration (odds + GateNumber).
     
        try
        {
            await _lifecycle.CloseRegistrationAsync(request.RaceId, cancellationToken);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
