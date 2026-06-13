using MediatR;

namespace Application.Usecases.Users.UpdateUser;

public sealed record UpdateUserCommand(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    int RoleId,
    bool IsActive,
    DateTime? LockedUntil,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,
    bool IsProfileComplete
) : IRequest<bool>;