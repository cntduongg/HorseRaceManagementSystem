using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Common.Wallet;
namespace Application.Usecases.Predictions.CreatePrediction;

public sealed class CreatePredictionCommandHandler : IRequestHandler<CreatePredictionCommand, int>
{
    private const decimal MinBet = 10m;
    private readonly IApplicationDbContext _context;
    public CreatePredictionCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CreatePredictionCommand request, CancellationToken cancellationToken)
    {
        if (request.BetAmount < MinBet) throw new InvalidOperationException($"Min bet is {MinBet} points.");

        var race = await _context.Races.FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != "Scheduled")
            throw new InvalidOperationException("You can only bet while the race is Scheduled.");

        if (race.OddsComputedAt == null)
            throw new InvalidOperationException("Registration must be closed and odds computed first.");

        // Cửa cược = [Admin publish odds → Admin khóa cược]. Trước khi publish, giá vẫn đang
        // được Admin chỉnh; sau khi khóa, sổ đã đóng để chuẩn bị xuất phát (Flow 7).
        if (race.OddsPublishedAt == null)
            throw new InvalidOperationException(
                "Odds have not been published yet. Betting opens once the Admin publishes the odds.");

        if (race.BettingLockedAt != null)
            throw new InvalidOperationException("Betting is locked for this race.");

        var spectator = await _context.Spectators.FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken)
            ?? throw new KeyNotFoundException("Spectator not found.");
        if (!spectator.IsActive) throw new InvalidOperationException("Spectator account is locked.");

        // Chỉ được cược 1 lần cho cả cuộc đua
        var hasActive = await _context.Predictions.AnyAsync(
            p => p.RaceId == request.RaceId && p.SpectatorId == request.SpectatorId && p.Status != PredictionStatus.Cancelled,
            cancellationToken);
        if (hasActive) throw new InvalidOperationException("You already have an active prediction for this race.");

        var wallet = await _context.PointWallets.FirstOrDefaultAsync(w => w.SpectatorId == request.SpectatorId, cancellationToken)
            ?? throw new KeyNotFoundException("Wallet not found.");
        if (wallet.Balance < request.BetAmount) throw new InvalidOperationException("Insufficient balance.");
        if (request.BetAmount > wallet.Balance * 0.5m) throw new InvalidOperationException("Cannot bet more than 50% of your balance.");
        var selectedEntry = await _context.Entries
    .Include(e => e.Horse)
    .Include(e => e.Jockey)
    .FirstOrDefaultAsync(
        e => e.EntryId == request.FirstEntryId &&
             e.RaceId == request.RaceId,
        cancellationToken)
    ?? throw new KeyNotFoundException("Selected entry not found.");

        if (selectedEntry.Status != "Approved")
            throw new InvalidOperationException("You can only bet on approved entries.");

        // Giá khóa = ĐÚNG con số spectator vừa nhìn thấy trên bảng (odds công bố của Admin).
        // Không tính lại, không điều chỉnh theo pool — đó chính là chỗ trước đây sinh ra
        // "bảng hiện 4.00x, lệnh khóa 2.00x".
        var odds = selectedEntry.PublishedOdds;
        if (odds <= 0)
            throw new InvalidOperationException("The selected entry does not have published odds.");

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var prediction = new Prediction
            {
                RaceId = request.RaceId,
                SpectatorId = request.SpectatorId,
                FirstEntryId = request.FirstEntryId,
                BetAmount = request.BetAmount,
                OddsLocked1 = odds,
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
                Reason = WalletTransactionReasonBuilder.BetPlaced(
                race,
                selectedEntry.Horse,
                selectedEntry.Jockey),
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