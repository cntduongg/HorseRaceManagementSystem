using MediatR;

namespace Application.Usecases.JockeyInvitations.DeleteJockeyInvitation;

public sealed class DeleteJockeyInvitationCommandHandler
    : IRequestHandler<DeleteJockeyInvitationCommand, bool>
{
    public Task<bool> Handle(
        DeleteJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}