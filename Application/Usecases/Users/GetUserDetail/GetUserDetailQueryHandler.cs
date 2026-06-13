using MediatR;

namespace Application.Usecases.Users.GetUserDetail;

public sealed class GetUserDetailQueryHandler
    : IRequestHandler<GetUserDetailQuery, UserDetailResponse?>
{
    public Task<UserDetailResponse?> Handle(
        GetUserDetailQuery request,
        CancellationToken cancellationToken)
    {
        var response = new UserDetailResponse(
            request.UserId,
            "admin@horserace.com",
            "System Admin",
            "0901234567",
            null,
            5,
            true,
            null,
            null,
            null,
            null,
            false
        );

        return Task.FromResult<UserDetailResponse?>(response);
    }
}