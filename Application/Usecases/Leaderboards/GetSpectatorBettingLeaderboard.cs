using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Leaderboards;

// Bảng xếp hạng cược của Spectator (Flow 7). Tính ON-READ từ Prediction.
//
// Lý do endpoint này tồn tại: trang Leaderboard của FE trước đây dựng bảng bằng cách tải
// **toàn bộ** `GET /api/predictions` về rồi tự gom nhóm — nghĩa là mọi khán giả (và cả REFEREE)
// đọc được từng lệnh cược đang chờ của người khác trong lúc race còn `Scheduled`. Sau khi
// `GET /api/predictions` bị giới hạn về "chỉ của mình", bảng xếp hạng cần một nguồn **chỉ chứa
// số liệu tổng hợp**: không lệnh cược lẻ, không số dư ví, không cửa đã đặt.
public sealed record GetSpectatorBettingLeaderboardQuery()
    : IQuery<List<SpectatorBettingLeaderboardItem>>;

public sealed record SpectatorBettingLeaderboardItem(
    int Rank,
    int SpectatorId,
    string FullName,
    int TotalBets,
    int WonBets,
    int WinRate,
    decimal TotalStaked,
    decimal TotalWinnings);

public sealed class GetSpectatorBettingLeaderboardQueryHandler
    : IRequestHandler<GetSpectatorBettingLeaderboardQuery, List<SpectatorBettingLeaderboardItem>>
{
    private readonly IApplicationDbContext _context;

    public GetSpectatorBettingLeaderboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpectatorBettingLeaderboardItem>> Handle(
        GetSpectatorBettingLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        // Cược đã hủy được hoàn 100% → không tính vào cả số lệnh lẫn tiền đặt, để Win Rate
        // chỉ phản ánh các lệnh đã có kết quả. (Giống cách FE đang gom trước đây.)
        var grouped = await _context.Predictions
            .AsNoTracking()
            .Where(p => p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.SpectatorId)
            .Select(g => new
            {
                SpectatorId = g.Key,
                TotalBets = g.Count(),
                WonBets = g.Count(x => x.Status == PredictionStatus.Won),
                TotalStaked = g.Sum(x => x.BetAmount),
                // Payout thực tế = BetAmount × OddsLocked1 (đúng công thức PublishRaceResult dùng
                // khi quyết toán). FE trước đây không có `oddsLocked1` trong response list nên
                // luôn nhân với 1 → tiền thắng bị báo thiếu.
                TotalWinnings = g.Sum(x =>
                    x.Status == PredictionStatus.Won ? x.BetAmount * x.OddsLocked1 : 0m)
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return new List<SpectatorBettingLeaderboardItem>();
        }

        var spectatorIds = grouped.Select(g => g.SpectatorId).ToList();
        var names = await _context.Users
            .AsNoTracking()
            .Where(u => spectatorIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FullName })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);

        return grouped
            .OrderByDescending(g => g.TotalWinnings)
            .ThenByDescending(g => g.WonBets)
            .ThenBy(g => g.SpectatorId)
            .Select((g, i) => new SpectatorBettingLeaderboardItem(
                Rank: i + 1,
                SpectatorId: g.SpectatorId,
                FullName: names.TryGetValue(g.SpectatorId, out var n) && !string.IsNullOrWhiteSpace(n)
                    ? n
                    : $"User #{g.SpectatorId}",
                TotalBets: g.TotalBets,
                WonBets: g.WonBets,
                WinRate: g.TotalBets > 0
                    ? (int)Math.Round(g.WonBets * 100.0 / g.TotalBets, MidpointRounding.AwayFromZero)
                    : 0,
                TotalStaked: g.TotalStaked,
                TotalWinnings: g.TotalWinnings))
            .ToList();
    }
}
