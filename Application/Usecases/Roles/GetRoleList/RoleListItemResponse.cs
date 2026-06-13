namespace Application.Usecases.Roles.GetRoleList;

public sealed record RoleListItemResponse(
	int RoleId,
	string Code,
	string Name
);