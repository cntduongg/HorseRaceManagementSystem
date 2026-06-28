using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Usecases.Predictions.Common;

namespace Application.Usecases.Predictions.CreatePrediction;

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
            throw new InvalidOperationException("EntryId is required.");

        if (request.BetAmount < MinBet)
            throw new InvalidOperationException($"Số điểm cược tối thiểu là {MinBet} điểm.");

        var race = await _context.Races
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        if (!string.Equals(race.Status?.Trim(), RaceScheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chỉ được đặt cược khi cuộc đua đang Scheduled. Current status: {race.Status}");
        }

        if (race.OddsComputedAt is null)
        {
            throw new InvalidOperationException(
                "Odds chưa được khóa. Admin cần close registration trước khi spectator đặt prediction.");
        }

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Spectator not found.");

        if (!spectator.IsActive)
            throw new InvalidOperationException("Tài khoản khán giả đang bị khóa.");

        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                e => e.EntryId == request.FirstEntryId &&
                     e.RaceId == request.RaceId,
                cancellationToken)
            ?? throw new InvalidOperationException("Entry không thuộc cuộc đua đã chọn.");

        if (!string.Equals(entry.Status?.Trim(), EntryApproved, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Entry chưa được duyệt. Current status: {entry.Status}");
        }

        if (entry.Odds <= 0)
        {
            throw new InvalidOperationException(
                "Entry chưa có locked odds hợp lệ.");
        }

        var hasActive = await _context.Predictions.AnyAsync(
            p => p.RaceId == request.RaceId &&
                 p.SpectatorId == request.SpectatorId &&
                 p.Status != PredictionStatus.Cancelled,
            cancellationToken);

        if (hasActive)
            throw new InvalidOperationException("Bạn đã có một dự đoán đang hoạt động cho cuộc đua này.");

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(w => w.SpectatorId == request.SpectatorId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy ví điểm.");

        if (wallet.IsFrozen)
            throw new InvalidOperationException("Ví điểm đang bị đóng băng.");

        if (wallet.Balance < request.BetAmount)
            throw new InvalidOperationException("Số dư không đủ.");

        var maxBet = wallet.Balance * 0.5m;

        if (request.BetAmount > maxBet)
            throw new InvalidOperationException("Số điểm cược không được vượt quá 50% số dư.");

        var odds = await PredictionOddsCalculator.CalculateEntryOddsAsync(
            _context,
            request.RaceId,
            request.FirstEntryId,
            request.BetAmount,
            cancellationToken);

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var prediction = new Prediction
            {
                RaceId = request.RaceId,
                SpectatorId = request.SpectatorId,
                FirstEntryId = request.FirstEntryId,
                SecondEntryId = null,
                ThirdEntryId = null,

                BetAmount = request.BetAmount,

                OddsLocked1 = odds,
                OddsLocked2 = null,
                OddsLocked3 = null,

                Status = PredictionStatus.Pending,
                CreatedAt = now
            };

            _context.Predictions.Add(prediction);

            await _context.SaveChangesAsync(cancellationToken);

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
                Reason = $"Đặt cược race #{request.RaceId}, entry #{request.FirstEntryId}, odds {odds}",
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
}