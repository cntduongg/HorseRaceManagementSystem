using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.GetUserList;

public sealed class GetUserListQueryHandler
    : IRequestHandler<GetUserListQuery, PagedUserListResponse>
{
    private readonly IApplicationDbContext _context;

    public GetUserListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedUserListResponse> Handle(
        GetUserListQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 1000);

        var query = _context.Users.AsNoTracking();

        // Search theo email / họ tên.
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FullName.ToLower().Contains(s));
        }

        // Filter role: hỗ trợ cả RoleId (số) lẫn Role.Code (chuỗi, vd "JOCKEY").
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            if (int.TryParse(role, out var roleId))
                query = query.Where(u => u.RoleId == roleId);
            else
                query = query.Where(u => u.Role.Code == role);
        }

        // Filter status: "active"/"inactive" -> IsActive; còn lại so theo User.Status.
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.IsActive);
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => !u.IsActive);
            else
                query = query.Where(u => u.Status == status);
        }

        // Sort (mặc định createdAt desc).
        var desc = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (request.Sort?.Trim().ToLower()) switch
        {
            "fullname" => desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "email" => desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            _ => desc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
        };

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemResponse(
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.RoleId,
                u.Role.Code,
                u.IsActive,
                u.Status,
                u.LockedUntil,
                u.LicenseNumber,
                u.Weight,
                u.Bio,
                u.IsProfileComplete,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedUserListResponse(items, total, page, pageSize);
    }
}
