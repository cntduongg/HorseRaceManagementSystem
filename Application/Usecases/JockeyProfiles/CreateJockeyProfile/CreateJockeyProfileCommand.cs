using MediatR;

namespace Application.Usecases.JockeyProfiles.CreateJockeyProfile;

public sealed record CreateJockeyProfileCommand(
    int UserId,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio
) : IRequest<int>;