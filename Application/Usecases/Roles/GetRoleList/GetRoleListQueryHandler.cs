using MediatR;

namespace Application.Usecases.Roles.GetRoleList;

public sealed class GetRoleListQueryHandler
    : IRequestHandler<GetRoleListQuery, List<RoleListItemResponse>>
{
    public Task<List<RoleListItemResponse>> Handle(
        GetRoleListQuery request,
        CancellationToken cancellationToken)
    {
        var roles = new List<RoleListItemResponse>
        {
            new(
                1,
                "ADMIN",
                "Administrator"
            ),
            new(
                2,
                "JOCKEY",
                "Jockey"
            )
        };

        return Task.FromResult(roles);
    }
}