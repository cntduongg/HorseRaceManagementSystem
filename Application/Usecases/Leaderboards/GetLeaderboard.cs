using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Leaderboards;

// Bảng xếp hạng Prize Points (Flow 8). Tính ON-READ từ PrizePointTransaction (nguồn sự thật) →
// tự nhất quán với Publish/Unpublish (transaction được thêm/gỡ).
//  TournamentId != null → Tournament Leaderboard; null → Career (toàn giải).
//  Role != null → lọc theo vai trò (JOCKEY / HORSE_OWNER).
public sealed record GetLeaderboardQuery(int? TournamentId, string? Role)
    : IQuery<List<LeaderboardItem>>;

public sealed record LeaderboardItem(
    int Rank,
    int UserId,
    string FullName,
    string Role,
    int PrizePoints);

public sealed class GetLeaderboardQueryHandler
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardItem>>
{
    private readonly IApplicationDbContext _context;

    public GetLeaderboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaderboardItem>> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PrizePointTransactions
            .AsNoTracking()
            .Where(p => p.TransactionType == PrizePointTransactionType.Awarded);

        if (request.TournamentId is > 0)
            query = query.Where(p => p.TournamentId == request.TournamentId);

        var grouped = await query
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Points) })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
            return new List<LeaderboardItem>();

        var userIds = grouped.Select(g => g.UserId).ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FullName, RoleCode = u.Role.Code })
            .ToDictionaryAsync(u => u.UserId, cancellationToken);

        var rows = grouped
            .Select(g =>
            {
                users.TryGetValue(g.UserId, out var u);
                return new
                {
                    g.UserId,
                    FullName = u?.FullName ?? $"User #{g.UserId}",
                    Role = u?.RoleCode ?? "",
                    g.Points
                };
            })
            .Where(x => string.IsNullOrWhiteSpace(request.Role) ||
                        string.Equals(x.Role, request.Role, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.FullName)
            .ToList();

        return rows
            .Select((x, i) => new LeaderboardItem(i + 1, x.UserId, x.FullName, x.Role, x.Points))
            .ToList();
    }
}
