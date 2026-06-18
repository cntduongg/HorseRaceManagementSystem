namespace Application.Usecases.Admin.GetPendingUsers;

public sealed record PendingUserResponse(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string RoleCode,
    string RoleName,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,
    DateTime CreatedAt
);
