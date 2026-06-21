using MediatR;

namespace Application.Usecases.Users.GetUserList;

public sealed class GetUserListQueryHandler
    : IRequestHandler<GetUserListQuery, List<UserListItemResponse>>
{
    public Task<List<UserListItemResponse>> Handle(
        GetUserListQuery request,
        CancellationToken cancellationToken)
    {
        var users = new List<UserListItemResponse>
        {
            new(
                1,
                "admin@horserace.com",
                "System Admin",
                5,
                true
            ),
            new(
                2,
                "jockey1@horserace.com",
                "Nguyen Van A",
                2,
                true
            )
        };

        return Task.FromResult(users);
    }
}