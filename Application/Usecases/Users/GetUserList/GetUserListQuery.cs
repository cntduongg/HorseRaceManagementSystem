using MediatR;

namespace Application.Usecases.Users.GetUserList;

// GET /api/users?page=&pageSize=&search=&role=&status=&sort=&sortDirection=
public sealed record GetUserListQuery(
	int Page = 1,
	int PageSize = 10,
	string? Search = null,
	string? Role = null,
	string? Status = null,
	string? Sort = null,
	string? SortDirection = null
) : IRequest<PagedUserListResponse>;
