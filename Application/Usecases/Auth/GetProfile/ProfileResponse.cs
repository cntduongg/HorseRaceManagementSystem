namespace Application.Usecases.Auth.GetProfile;

public sealed record ProfileResponse(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string Role,
    string Status,
    bool IsActive,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,
    bool IsProfileComplete
);
