using MediatR;

namespace Application.Usecases.JockeyProfiles.DeleteJockeyProfile;

public sealed class DeleteJockeyProfileCommandHandler
    : IRequestHandler<DeleteJockeyProfileCommand, bool>
{
    public Task<bool> Handle(
        DeleteJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete JockeyProfile from database

        return Task.FromResult(true);
    }
}