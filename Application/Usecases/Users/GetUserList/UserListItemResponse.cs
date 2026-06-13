namespace Application.Usecases.Users.GetUserList;

public sealed record UserListItemResponse(
	int UserId,
	string Email,
	string FullName,
	int RoleId,
	bool IsActive
);