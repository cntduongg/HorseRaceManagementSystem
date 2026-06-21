using MediatR;

namespace Application.Usecases.JockeyProfiles.DeleteJockeyProfile;

public sealed record DeleteJockeyProfileCommand(
    int UserId
) : IRequest<bool>;