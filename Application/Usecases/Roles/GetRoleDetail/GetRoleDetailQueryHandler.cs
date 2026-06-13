using MediatR;

namespace Application.Usecases.Roles.GetRoleDetail;

public sealed class GetRoleDetailQueryHandler
    : IRequestHandler<GetRoleDetailQuery, RoleDetailResponse?>
{
    public Task<RoleDetailResponse?> Handle(
        GetRoleDetailQuery request,
        CancellationToken cancellationToken)
    {
        var response = new RoleDetailResponse(
            request.RoleId,
            "ADMIN",
            "Administrator"
        );

        return Task.FromResult<RoleDetailResponse?>(response);
    }
}