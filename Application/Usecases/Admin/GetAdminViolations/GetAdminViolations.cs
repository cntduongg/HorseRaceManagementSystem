using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.GetAdminViolations;

// GET /api/admin/violations?status=&page=&pageSize=&search=&sort=&sortDirection=
// Search khớp: loại vi phạm, mô tả, tên race, tên nài, tên ngựa.
public sealed record GetAdminViolationsQuery(
    string? Status,
    int Page,
    int PageSize,
    string? Search = null,
    string? Sort = null,
    string? SortDirection = null)
    : IRequest<AdminViolationsResult>;

public sealed record AdminViolationItem(
    int ViolationId,
    int RaceId,
    string? RaceName,
    int LegNumber,
    int EntryId,
    int ReportedByRefereeId,
    string ViolatorName,
    string ViolatorRole,
    string ViolationType,
    string? Description,
    string Penalty,
    string Status,
    string? ResolvedByAdminName,
    DateTime? ResolvedAt,
    string? AdminNote,
    DateTime CreatedAt);

public sealed record AdminViolationsResult(
    IReadOnlyList<AdminViolationItem> Items,
    int Total,
    int PendingCount,
    int ResolvedCount);

public sealed class GetAdminViolationsQueryHandler
    : IRequestHandler<GetAdminViolationsQuery, AdminViolationsResult>
{
    private readonly IApplicationDbContext _context;

    public GetAdminViolationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminViolationsResult> Handle(
        GetAdminViolationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize is <= 0 or > 200 ? 15 : request.PageSize;

        var query = _context.Violations.AsNoTracking();

        // FE status (Pending/Resolved/Dismissed) → domain status (Pending/Approved/Rejected).
        var domainStatus = request.Status switch
        {
            "Resolved" => "Approved",
            "Dismissed" => "Rejected",
            "Pending" => "Pending",
            _ => null
        };
        if (domainStatus is not null)
            query = query.Where(v => v.Status == domainStatus);

        // Search — dùng ToLower().Contains (không dùng ILike của Npgsql) để chạy được
        // trên cả PostgreSQL lẫn InMemory provider trong test.
        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(v =>
                v.ViolationType.ToLower().Contains(term) ||
                (v.Description != null && v.Description.ToLower().Contains(term)) ||
                v.Entry.Race.Name.ToLower().Contains(term) ||
                v.Entry.Jockey.FullName.ToLower().Contains(term) ||
                v.Entry.Horse.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var pendingCount = await _context.Violations.CountAsync(v => v.Status == "Pending", cancellationToken);
        var resolvedCount = await _context.Violations.CountAsync(v => v.Status == "Approved", cancellationToken);

        // Sort — mặc định mới nhất trước; ViolationId làm tie-break để phân trang ổn định.
        var desc = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var ordered = request.Sort?.Trim().ToLowerInvariant() switch
        {
            "violationtype" => desc ? query.OrderByDescending(v => v.ViolationType) : query.OrderBy(v => v.ViolationType),
            "status"        => desc ? query.OrderByDescending(v => v.Status) : query.OrderBy(v => v.Status),
            "penalty"       => desc ? query.OrderByDescending(v => v.Penalty) : query.OrderBy(v => v.Penalty),
            "legnumber"     => desc ? query.OrderByDescending(v => v.LegNumber) : query.OrderBy(v => v.LegNumber),
            "racename"      => desc ? query.OrderByDescending(v => v.Entry.Race.Name) : query.OrderBy(v => v.Entry.Race.Name),
            _               => desc ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt),
        };

        var rows = await ordered
            .ThenByDescending(v => v.ViolationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.ViolationId,
                v.RaceId,
                RaceName = v.Entry.Race.Name,
                v.LegNumber,
                v.EntryId,
                v.ReportedByRefereeId,
                ViolatorName = v.Entry.Jockey.FullName,
                ViolatorRole = v.Entry.Jockey.Role.Code,
                HorseName = v.Entry.Horse.Name,
                v.ViolationType,
                v.Description,
                v.Penalty,
                v.Status,
                v.ReviewedAt,
                v.ReviewedByAdminId,
                v.AdminNote,
                v.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Tên admin đã xử lý.
        var adminIds = rows.Where(r => r.ReviewedByAdminId != null)
            .Select(r => r.ReviewedByAdminId!.Value).Distinct().ToList();
        var adminNames = await _context.Users
            .Where(u => adminIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);

        var items = rows.Select(r => new AdminViolationItem(
            r.ViolationId,
            r.RaceId,
            r.RaceName,
            r.LegNumber,
            r.EntryId,
            r.ReportedByRefereeId,
            $"{r.ViolatorName} ({r.HorseName})",
            r.ViolatorRole,
            r.ViolationType,
            r.Description,
            // Rejected → không áp phạt: chuẩn hóa Penalty="None" cho FE.
            r.Status == "Rejected" ? "None" : r.Penalty,
            r.Status switch { "Approved" => "Resolved", "Rejected" => "Dismissed", _ => "Pending" },
            r.ReviewedByAdminId != null && adminNames.TryGetValue(r.ReviewedByAdminId.Value, out var n) ? n : null,
            r.ReviewedAt,
            r.AdminNote,
            r.CreatedAt))
            .ToList();

        return new AdminViolationsResult(items, total, pendingCount, resolvedCount);
    }
}
