using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Roles.GetRoleDetail;

public sealed class GetRoleDetailQueryHandler
    : IRequestHandler<GetRoleDetailQuery, RoleDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetRoleDetailQueryHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoleDetailResponse?> Handle(
        GetRoleDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Where(x => x.RoleId == request.RoleId)
            .Select(x => new RoleDetailResponse(
                x.RoleId,
                x.Code,
                x.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}