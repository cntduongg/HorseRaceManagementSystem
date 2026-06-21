using MediatR;

namespace Application.Usecases.Spectators.UpdateSpectator;

public sealed class UpdateSpectatorCommandHandler
    : IRequestHandler<UpdateSpectatorCommand, bool>
{
    public Task<bool> Handle(
        UpdateSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update spectator in database

        return Task.FromResult(true);
    }
}