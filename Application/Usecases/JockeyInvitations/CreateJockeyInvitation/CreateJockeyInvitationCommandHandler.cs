using MediatR;

namespace Application.Usecases.JockeyInvitations.CreateJockeyInvitation;

public sealed class CreateJockeyInvitationCommandHandler
    : IRequestHandler<CreateJockeyInvitationCommand, int>
{
    public Task<int> Handle(
        CreateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseOwnerId <= 0)
        {
            throw new InvalidOperationException(
                "HorseOwnerId is required.");
        }

        if (request.JockeyId <= 0)
        {
            throw new InvalidOperationException(
                "JockeyId is required.");
        }

        if (request.HorseId <= 0)
        {
            throw new InvalidOperationException(
                "HorseId is required.");
        }

        if (request.RaceId <= 0)
        {
            throw new InvalidOperationException(
                "RaceId is required.");
        }

        // TODO: Save JockeyInvitation into database

        var invitationId = 1;

        return Task.FromResult(invitationId);
    }
}