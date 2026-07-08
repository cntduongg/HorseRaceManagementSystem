using Application.Common.Interfaces;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.GetUserHistory;

// GET /api/admin/users/{id}/history
// Timeline hoạt động của 1 user: giao dịch ví (Spectator), điểm thưởng (Owner/Jockey),
// và lịch sử duyệt hồ sơ (ReviewHistory). Gộp thành một danh sách sắp theo thời gian giảm dần.
public sealed record GetUserHistoryQuery(int UserId, int Page, int PageSize)
    : IRequest<UserHistoryResult>;

public sealed record UserHistoryItem(
    string Category,      // Wallet | Prize | Review
    string Type,
    decimal? Amount,
    string? Reason,
    DateTime CreatedAt);

public sealed record UserHistoryResult(
    IReadOnlyList<UserHistoryItem> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class GetUserHistoryQueryHandler
    : IRequestHandler<GetUserHistoryQuery, UserHistoryResult>
{
    private readonly IApplicationDbContext _context;

    public GetUserHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserHistoryResult> Handle(
        GetUserHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 200);

        var wallet = await _context.WalletTransactions
            .AsNoTracking()
            .Where(w => w.SpectatorId == request.UserId)
            .Select(w => new UserHistoryItem("Wallet", w.Type, w.Amount, w.Reason, w.CreatedAt))
            .ToListAsync(cancellationToken);

        var prize = await _context.PrizePointTransactions
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Select(p => new UserHistoryItem(
                "Prize",
                p.SourceType,
                p.Points,
                "Race " + p.RaceId + " · vị trí " + p.FinalPosition,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        var review = await _context.ReviewHistories
            .AsNoTracking()
            .Where(r => r.EntityType == ReviewEntity.User && r.EntityId == request.UserId)
            .Select(r => new UserHistoryItem(
                "Review",
                r.Action.ToString(),
                null,
                r.Reason,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        var all = wallet
            .Concat(prize)
            .Concat(review)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new UserHistoryResult(items, all.Count, page, pageSize);
    }
}
