using MediatR;

namespace Application.Usecases.Spectators.CreateSpectator;

public sealed class CreateSpectatorCommandHandler
    : IRequestHandler<CreateSpectatorCommand, int>
{
    public Task<int> Handle(
        CreateSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            throw new InvalidOperationException("UserId is required.");
        }

        // TODO: Save spectator into database

        return Task.FromResult(request.UserId);
    }
}