using MediatR;

namespace Application.Usecases.Spectators.DeleteSpectator;

public sealed class DeleteSpectatorCommandHandler
    : IRequestHandler<DeleteSpectatorCommand, bool>
{
    public Task<bool> Handle(
        DeleteSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete spectator from database

        return Task.FromResult(true);
    }
}