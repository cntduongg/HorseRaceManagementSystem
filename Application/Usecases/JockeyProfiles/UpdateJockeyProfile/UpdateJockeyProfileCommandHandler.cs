using MediatR;

namespace Application.Usecases.JockeyProfiles.UpdateJockeyProfile;

public sealed class UpdateJockeyProfileCommandHandler
    : IRequestHandler<UpdateJockeyProfileCommand, bool>
{
    public Task<bool> Handle(
        UpdateJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update JockeyProfile in database

        return Task.FromResult(true);
    }
}