using MediatR;

namespace Application.Usecases.JockeyProfiles.CreateJockeyProfile;

public sealed class CreateJockeyProfileCommandHandler
    : IRequestHandler<CreateJockeyProfileCommand, int>
{
    public Task<int> Handle(
        CreateJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            throw new InvalidOperationException(
                "UserId is required.");
        }

        // TODO: Save JockeyProfile into database

        return Task.FromResult(request.UserId);
    }
}