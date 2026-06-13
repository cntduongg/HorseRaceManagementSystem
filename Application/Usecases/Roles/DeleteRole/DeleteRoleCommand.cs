using MediatR;

namespace Application.Usecases.Roles.DeleteRole;

public sealed record DeleteRoleCommand(
    int RoleId
) : IRequest<bool>;