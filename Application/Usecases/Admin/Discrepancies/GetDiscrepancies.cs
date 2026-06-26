using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.Discrepancies;

// GET /api/admin/discrepancies?status=&page=&pageSize=
public sealed record GetDiscrepanciesQuery(string? Status, int Page, int PageSize)
    : IRequest<DiscrepanciesResult>;

public sealed record PositionInfo(int? PredictedPosition, int? OfficialPosition);

public sealed record DiscrepancyItem(
    int DiscrepancyId,
    int? RaceId,
    string? RaceName,
    DateTime? RaceDate,
    string? ReportedByName,
    string? ReportedByEmail,
    string? ReportedByRole,
    DateTime ReportedAt,
    string Type,
    string Status,
    string? Description,
    PositionInfo? UserPrediction,
    PositionInfo? OfficialResult,
    string? Resolution,
    int? AdjustedPointsAwarded,
    string? ResolvedByAdminName,
    DateTime? ResolvedAt);

public sealed record DiscrepanciesResult(
    IReadOnlyList<DiscrepancyItem> Items,
    int Total,
    int PendingCount,
    int ResolvedCount);

public sealed class GetDiscrepanciesQueryHandler
    : IRequestHandler<GetDiscrepanciesQuery, DiscrepanciesResult>
{
    private readonly IApplicationDbContext _context;

    public GetDiscrepanciesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DiscrepanciesResult> Handle(
        GetDiscrepanciesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize is <= 0 or > 200 ? 15 : request.PageSize;

        var query = _context.Discrepancies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
            query = query.Where(d => d.Status == request.Status);

        var total = await query.CountAsync(cancellationToken);
        var pendingCount = await _context.Discrepancies.CountAsync(d => d.Status == "Pending", cancellationToken);
        var resolvedCount = await _context.Discrepancies.CountAsync(d => d.Status == "Resolved", cancellationToken);

        var rows = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var raceIds = rows.Where(r => r.RaceId != null).Select(r => r.RaceId!.Value).Distinct().ToList();
        var userIds = rows.SelectMany(r => new[] { r.ReportedById, r.ResolvedByAdminId })
            .Where(id => id != null).Select(id => id!.Value).Distinct().ToList();

        var races = await _context.Races.AsNoTracking()
            .Where(r => raceIds.Contains(r.RaceId))
            .Select(r => new { r.RaceId, r.Name, r.ScheduledStartTime })
            .ToDictionaryAsync(r => r.RaceId, cancellationToken);

        var users = await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Include(u => u.Role)
            .Select(u => new { u.UserId, u.FullName, u.Email, RoleCode = u.Role.Code })
            .ToDictionaryAsync(u => u.UserId, cancellationToken);

        var items = rows.Select(d =>
        {
            races.TryGetValue(d.RaceId ?? 0, out var race);
            var reporter = d.ReportedById != null && users.TryGetValue(d.ReportedById.Value, out var rp) ? rp : null;
            var admin = d.ResolvedByAdminId != null && users.TryGetValue(d.ResolvedByAdminId.Value, out var ad) ? ad : null;

            return new DiscrepancyItem(
                d.DiscrepancyId,
                d.RaceId,
                race?.Name,
                race?.ScheduledStartTime,
                reporter?.FullName,
                reporter?.Email,
                reporter?.RoleCode,
                d.CreatedAt,
                d.Type,
                d.Status,
                d.Description,
                new PositionInfo(d.PredictedPosition, null),
                new PositionInfo(null, d.OfficialPosition),
                d.Resolution,
                d.AdjustedPointsAwarded,
                admin?.FullName,
                d.ResolvedAt);
        }).ToList();

        return new DiscrepanciesResult(items, total, pendingCount, resolvedCount);
    }
}
