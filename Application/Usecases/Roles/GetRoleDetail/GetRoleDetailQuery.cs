using MediatR;

namespace Application.Usecases.Roles.GetRoleDetail;

public sealed record GetRoleDetailQuery(int RoleId)
    : IRequest<RoleDetailResponse?>;