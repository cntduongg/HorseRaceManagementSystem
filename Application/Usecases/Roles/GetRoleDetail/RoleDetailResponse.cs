namespace Application.Usecases.Roles.GetRoleDetail;

public sealed record RoleDetailResponse(
    int RoleId,
    string Code,
    string Name
);