using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.GetUserDetail;

public sealed class GetUserDetailQueryHandler
    : IRequestHandler<GetUserDetailQuery, UserDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetUserDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDetailResponse?> Handle(
        GetUserDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(x => x.UserId == request.UserId)
            .Select(x => new UserDetailResponse(
                x.UserId,
                x.Email,
                x.FullName,
                x.PhoneNumber,
                x.AvatarUrl,
                x.RoleId,
                x.IsActive,
                x.LockedUntil,
                x.LicenseNumber,
                x.Weight,
                x.Bio,
                x.IsProfileComplete
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}