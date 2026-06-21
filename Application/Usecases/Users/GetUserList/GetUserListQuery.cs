using MediatR;

namespace Application.Usecases.Users.GetUserList;

public sealed record GetUserListQuery()
	: IRequest<List<UserListItemResponse>>;