using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// Flow 7 — Cộng +100 cho ví Spectator mỗi thứ Hai 00:00 (UTC). Idempotent: chỉ cộng nếu tuần này
// chưa có giao dịch "WeeklyTopUp" (không cần cột marker → không cần migration). Ví đóng băng bị bỏ qua.
public sealed record RunWeeklyTopUpCommand() : ICommand<RunWeeklyTopUpResponse>;

public sealed record RunWeeklyTopUpResponse(int WalletsToppedUp, DateTime WeekStart);

public sealed class RunWeeklyTopUpCommandHandler
    : IRequestHandler<RunWeeklyTopUpCommand, RunWeeklyTopUpResponse>
{
    private const decimal WeeklyAmount = 100m;

    private readonly IApplicationDbContext _context;

    public RunWeeklyTopUpCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RunWeeklyTopUpResponse> Handle(
        RunWeeklyTopUpCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var weekStart = MostRecentMondayUtc(now);

        // Bỏ qua ví đóng băng, và ví MỚI TẠO trong tuần này (đã nhận 100 điểm khởi tạo lúc đăng ký)
        // → tránh cộng thêm +100 ngay trong tuần đăng ký (sẽ nhận vào thứ Hai tuần kế).
        var wallets = await _context.PointWallets
            .Where(w => !w.IsFrozen && w.CreatedAt < weekStart)
            .ToListAsync(cancellationToken);
        if (wallets.Count == 0)
            return new RunWeeklyTopUpResponse(0, weekStart);

        var walletIds = wallets.Select(w => w.WalletId).ToList();
        var alreadyTopped = (await _context.WalletTransactions
            .Where(t => walletIds.Contains(t.WalletId) &&
                        t.Type == "WeeklyTopUp" &&
                        t.CreatedAt >= weekStart)
            .Select(t => t.WalletId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var count = 0;
        foreach (var w in wallets)
        {
            if (alreadyTopped.Contains(w.WalletId)) continue;

            w.Balance += WeeklyAmount;
            w.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = w.WalletId,
                SpectatorId = w.SpectatorId,
                Type = "WeeklyTopUp",
                Amount = WeeklyAmount,
                BalanceAfter = w.Balance,
                Reason = "Nạp điểm hàng tuần (+100)",
                CreatedAt = now
            });
            count++;
        }

        if (count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return new RunWeeklyTopUpResponse(count, weekStart);
    }

    // Thứ Hai gần nhất, 00:00 UTC (tuần hiện tại).
    private static DateTime MostRecentMondayUtc(DateTime nowUtc)
    {
        var daysSinceMonday = ((int)nowUtc.DayOfWeek + 6) % 7; // Monday = 0
        return nowUtc.Date.AddDays(-daysSinceMonday);
    }
}
