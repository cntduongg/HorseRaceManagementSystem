using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Roles.GetRoleList;

public sealed class GetRoleListQueryHandler
    : IRequestHandler<GetRoleListQuery, List<RoleListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetRoleListQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleListItemResponse>> Handle(
        GetRoleListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Select(x => new RoleListItemResponse(
                x.RoleId,
                x.Code,
                x.Name))
            .ToListAsync(cancellationToken);
    }
}