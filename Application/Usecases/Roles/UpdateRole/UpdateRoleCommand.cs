using MediatR;

namespace Application.Usecases.Roles.UpdateRole;

public sealed record UpdateRoleCommand(
    int RoleId,
    string Code,
    string Name
) : IRequest<bool>;