using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.CreatePrediction;

// Flow 7 — Đặt cược dự đoán: odds KHÓA SERVER-SIDE, trừ ví ngay, validate min 10 / 50% số dư /
// 1 dự đoán active mỗi race / race phải Scheduled. (Bỏ qua odds do client gửi.)
public sealed class CreatePredictionCommandHandler
    : IRequestHandler<CreatePredictionCommand, int>
{
    private const decimal MinBet = 10m;
    private const string RaceScheduled = "Scheduled";
    private const string EntryApproved = "Approved";

    private readonly IApplicationDbContext _context;

    public CreatePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");
        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");
        if (request.FirstEntryId <= 0)
            throw new InvalidOperationException("FirstEntryId is required.");
        if (request.BetAmount < MinBet)
            throw new InvalidOperationException($"Số tiền cược tối thiểu là {MinBet} điểm.");

        var race = await _context.Races
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        if (race.Status != RaceScheduled)
            throw new InvalidOperationException("Chỉ được đặt cược khi cuộc đua đang Scheduled.");

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Spectator not found.");
        if (!spectator.IsActive)
            throw new InvalidOperationException("Tài khoản khán giả đang bị khóa.");

        // Entry dự đoán về 1st phải thuộc race & đã duyệt.
        var firstEntry = await _context.Entries
            .FirstOrDefaultAsync(
                e => e.EntryId == request.FirstEntryId && e.RaceId == request.RaceId,
                cancellationToken)
            ?? throw new InvalidOperationException("Entry không thuộc cuộc đua đã chọn.");
        if (firstEntry.Status != EntryApproved)
            throw new InvalidOperationException("Entry chưa được duyệt.");

        // Tối đa 1 dự đoán active mỗi race.
        var hasActive = await _context.Predictions.AnyAsync(
            p => p.RaceId == request.RaceId &&
                 p.SpectatorId == request.SpectatorId &&
                 p.Status == "Pending",
            cancellationToken);
        if (hasActive)
            throw new InvalidOperationException("Bạn đã có một dự đoán đang hoạt động cho cuộc đua này.");

        // Ví: phải tồn tại, không đóng băng, đủ số dư, không vượt 50%.
        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(w => w.SpectatorId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy ví điểm.");
        if (wallet.IsFrozen)
            throw new InvalidOperationException("Ví điểm đang bị đóng băng.");
        if (request.BetAmount > wallet.Balance)
            throw new InvalidOperationException("Số dư không đủ.");
        if (request.BetAmount > wallet.Balance * 0.5m)
            throw new InvalidOperationException("Số tiền cược không được vượt quá 50% số dư.");

        // ── Odds server-side: ưu tiên odds đã KHÓA khi đóng đăng ký (Flow 3); fallback tính tạm ──
        var odds = firstEntry.Odds > 0
            ? firstEntry.Odds
            : await ComputeOddsAsync(request.RaceId, firstEntry.HorseId, cancellationToken);

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var prediction = new Prediction
            {
                RaceId = request.RaceId,
                SpectatorId = request.SpectatorId,
                FirstEntryId = request.FirstEntryId,
                SecondEntryId = request.FirstEntryId, // spec: chỉ dự đoán 1st
                ThirdEntryId = request.FirstEntryId,
                BetAmount = request.BetAmount,
                OddsLocked1 = odds,
                OddsLocked2 = odds,
                OddsLocked3 = odds,
                Status = "Pending",
                CreatedAt = now
            };
            _context.Predictions.Add(prediction);
            await _context.SaveChangesAsync(cancellationToken); // lấy PredictionId

            // Trừ ví ngay + ghi giao dịch.
            wallet.Balance -= request.BetAmount;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = request.SpectatorId,
                PredictionId = prediction.PredictionId,
                Type = "BetPlaced",
                Amount = -request.BetAmount,
                BalanceAfter = wallet.Balance,
                Reason = $"Đặt cược race #{request.RaceId}",
                CreatedAt = now
            });
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return prediction.PredictionId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Odds = 1 / winRate (clamp). winRate theo lịch sử về nhất của ngựa; chưa có lịch sử → theo cỡ field.
    private async Task<decimal> ComputeOddsAsync(int raceId, int horseId, CancellationToken ct)
    {
        var horseResults = await _context.RaceResults
            .Where(r => r.Entry.HorseId == horseId && r.FinalPosition != null)
            .Select(r => r.FinalPosition)
            .ToListAsync(ct);

        double winRate;
        if (horseResults.Count > 0)
        {
            var firsts = horseResults.Count(p => p == 1);
            winRate = (double)firsts / horseResults.Count;
        }
        else
        {
            var fieldSize = await _context.Entries.CountAsync(
                e => e.RaceId == raceId && e.Status == EntryApproved, ct);
            winRate = 1.0 / Math.Max(fieldSize, 2);
        }

        var odds = (decimal)Math.Round(1.0 / Math.Max(winRate, 0.04), 2);
        return Math.Clamp(odds, 1.1m, 25m);
    }
}
