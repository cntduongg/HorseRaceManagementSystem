using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.GetUserList;

public sealed class GetUserListQueryHandler
    : IRequestHandler<GetUserListQuery, List<UserListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetUserListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserListItemResponse>> Handle(
        GetUserListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .Select(x => new UserListItemResponse(
                x.UserId,
                x.Email,
                x.FullName,
                x.RoleId,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}