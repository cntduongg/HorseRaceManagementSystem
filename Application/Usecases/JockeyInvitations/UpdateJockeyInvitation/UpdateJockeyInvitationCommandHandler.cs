using MediatR;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

public sealed class UpdateJockeyInvitationCommandHandler
    : IRequestHandler<UpdateJockeyInvitationCommand, bool>
{
    public Task<bool> Handle(
        UpdateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}