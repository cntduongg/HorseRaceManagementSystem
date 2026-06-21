namespace Application.Usecases.Users.GetUserDetail;

public sealed record UserDetailResponse(
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
);