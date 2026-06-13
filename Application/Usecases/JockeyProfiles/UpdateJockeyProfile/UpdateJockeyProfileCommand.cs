using MediatR;

namespace Application.Usecases.JockeyProfiles.UpdateJockeyProfile;

public sealed record UpdateJockeyProfileCommand(
    int UserId,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio
) : IRequest<bool>;