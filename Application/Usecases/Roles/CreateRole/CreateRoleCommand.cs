using MediatR;

namespace Application.Usecases.Roles.CreateRole;

public sealed record CreateRoleCommand(
    string Code,
    string Name
) : IRequest<int>;