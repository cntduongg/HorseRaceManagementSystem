using Application.Common;

namespace Application.Usecases.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    string RoleCode,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio
) : ICommand<RegisterResponse>;
