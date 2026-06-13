using MediatR;

namespace Application.Usecases.Users.CreateUser;

public sealed record CreateUserCommand(
	string Email,
	string PasswordHash,
	string FullName,
	string? PhoneNumber,
	string? AvatarUrl,
	int RoleId,
	string? LicenseNumber,
	decimal? Weight,
	string? Bio
) : IRequest<int>;