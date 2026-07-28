using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

/// <summary>
/// Nạp bù ví spectator có số dư dưới 10 điểm lên đúng 10 điểm (idempotent theo ngày UTC, giao dịch Type = DailyTopUp).
/// Không có background worker — chỉ chạy khi Admin gọi <c>POST /api/admin/points/daily-topup</c> (cứu trợ thủ công).
/// Khác với <see cref="RunWeeklyTopUpCommand"/>, được <c>WeeklyTopUpBackgroundService</c> chạy tự động mỗi thứ Hai.
/// </summary>
public sealed record RunDailyTopUpCommand() : ICommand<RunDailyTopUpResponse>;
public sealed record RunDailyTopUpResponse(int WalletsToppedUp, DateTime DayStart);

public sealed class RunDailyTopUpCommandHandler : IRequestHandler<RunDailyTopUpCommand, RunDailyTopUpResponse>
{
    private const decimal DailyTargetAmount = 10m; 
    private readonly IApplicationDbContext _context;

    public RunDailyTopUpCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<RunDailyTopUpResponse> Handle(RunDailyTopUpCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dayStart = now.Date; 

        var wallets = await _context.PointWallets
            .Where(w => !w.IsFrozen && w.Balance < DailyTargetAmount)
            .ToListAsync(cancellationToken);

        if (wallets.Count == 0) return new RunDailyTopUpResponse(0, dayStart);

        var walletIds = wallets.Select(w => w.WalletId).ToList();
        var alreadyToppedToday = (await _context.WalletTransactions
            .Where(t => walletIds.Contains(t.WalletId) && t.Type == "DailyTopUp" && t.CreatedAt >= dayStart)
            .Select(t => t.WalletId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var count = 0;
        foreach (var w in wallets)
        {
            if (alreadyToppedToday.Contains(w.WalletId)) continue;

            var topUpAmount = DailyTargetAmount - w.Balance; 
            if (topUpAmount <= 0) continue;

            w.Balance += topUpAmount;
            w.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = w.WalletId,
                SpectatorId = w.SpectatorId,
                Type = "DailyTopUp",
                Amount = topUpAmount,
                BalanceAfter = w.Balance,
                Reason = $"Manual daily recovery top-up (+{topUpAmount})",
                CreatedAt = now
            });
            count++;
        }

        if (count > 0) await _context.SaveChangesAsync(cancellationToken);
        return new RunDailyTopUpResponse(count, dayStart);
    }
}