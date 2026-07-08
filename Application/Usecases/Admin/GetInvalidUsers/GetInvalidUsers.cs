using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.GetInvalidUsers;

// GET /api/admin/users/invalid
// "Invalid users" = tài khoản KHÔNG hoạt động được (IsActive == false): bị từ chối (Status=Rejected)
// hoặc bị khóa (LockedUntil). Màn admin để rà soát & khôi phục (approve) hoặc giữ từ chối (reject).
// Lưu ý: đây là tiện ích tùy chọn (không thuộc 8 Main Flow); approve/reject dùng lại
// ApproveUserCommand/RejectUserCommand.
public sealed record GetInvalidUsersQuery(string? Search, int Page, int PageSize)
    : IRequest<InvalidUsersResult>;

public sealed record InvalidUserItem(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    int RoleId,
    string? RoleCode,
    string Status,
    bool IsActive,
    DateTime? LockedUntil,
    DateTime CreatedAt);

public sealed record InvalidUsersResult(
    IReadOnlyList<InvalidUserItem> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class GetInvalidUsersQueryHandler
    : IRequestHandler<GetInvalidUsersQuery, InvalidUsersResult>
{
    private readonly IApplicationDbContext _context;

    public GetInvalidUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InvalidUsersResult> Handle(
        GetInvalidUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);

        // Không hoạt động được: bị vô hiệu hoặc đang bị khóa.
        var query = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsActive || (u.LockedUntil != null && u.LockedUntil > DateTime.UtcNow));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FullName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new InvalidUserItem(
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.RoleId,
                u.Role.Code,
                u.Status,
                u.IsActive,
                u.LockedUntil,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new InvalidUsersResult(items, total, page, pageSize);
    }
}
