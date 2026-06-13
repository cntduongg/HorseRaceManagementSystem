using MediatR;

namespace Application.Usecases.Roles.GetRoleList;

public sealed record GetRoleListQuery()
	: IRequest<List<RoleListItemResponse>>;