using MediatR;

namespace Application.Usecases.Users.GetUserDetail;

public sealed record GetUserDetailQuery(
    int UserId
) : IRequest<UserDetailResponse?>;