using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Application.Common.Wallet;
namespace Application.Usecases.Admin.RevokeHorse;

public class RevokeHorseCommandHandler
    : IRequestHandler<RevokeHorseCommand, RevokeHorseResponse>
{
    private static readonly string[] AllowedRaceStatusesForRevoke =
    {
        RaceStatus.Scheduled,
        RaceStatus.Finished,
        RaceStatus.Cancelled
    };

    private const string DefaultRevokeReason = "Revoked by admin.";

    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public RevokeHorseCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<RevokeHorseResponse> Handle(
        RevokeHorseCommand request,
        CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? DefaultRevokeReason
            : request.Reason.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var horse = await _context.Horses
                .Include(h => h.Entries)
                    .ThenInclude(e => e.Race)
                .FirstOrDefaultAsync(h => h.HorseId == request.HorseId, cancellationToken);

            if (horse is null)
                throw new KeyNotFoundException("Horse not found");

            if (horse.Status != HorseStatus.Approved)
                throw new InvalidOperationException("Only approved horse can be revoked");

            var hasEntryInActiveRace = horse.Entries.Any(e =>
                e.Status != EntryStatus.Cancelled &&
                !AllowedRaceStatusesForRevoke.Contains(e.Race.Status));

            if (hasEntryInActiveRace)
            {
                throw new InvalidOperationException(
                    "The horse has an Entry in an ongoing race and cannot be revoked.");
            }

            horse.Status = HorseStatus.Revoked;
            horse.RejectionReason = reason;
            horse.UpdatedAt = DateTime.UtcNow;

            var cancelled = 0;
            var now = DateTime.UtcNow;

            foreach (var entry in horse.Entries.Where(e => e.Status != EntryStatus.Cancelled))
            {
                var race = entry.Race;
                if (!AllowedRaceStatusesForRevoke.Contains(race.Status))
                    continue;

                if (race.OddsComputedAt != null)
                    await RefundPredictionsForEntryAsync(entry.EntryId, now, cancellationToken);

                entry.Status = EntryStatus.Cancelled;
                // Trả gate về cho race — entry đã hủy không được chiếm slot trong unique index
                // (RaceId, GateNumber) nữa. Odds của các entry CÒN LẠI thì giữ nguyên: giá đã
                // công bố là giá spectator đã cược theo, tính lại là đổi luật giữa chừng.
                entry.GateNumber = null;
                entry.UpdatedAt = now;
                cancelled++;
            }

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Horse,
                    EntityId = horse.HorseId,
                    Action = ReviewAction.Revoked,
                    Reason = reason,
                    AdminId = request.AdminId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RevokeHorseResponse(
                horse.HorseId,
                horse.Status,
                cancelled,
                new List<int>());
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task RefundPredictionsForEntryAsync(int entryId, DateTime now, CancellationToken ct)
    {
        var predictions = await _context.Predictions
             .Include(p => p.Race)
    .Include(p => p.FirstEntry)
        .ThenInclude(e => e.Horse)
    .Include(p => p.FirstEntry)
        .ThenInclude(e => e.Jockey)
            .Where(p => p.FirstEntryId == entryId &&
                        (p.Status == PredictionStatus.Pending || p.Status == PredictionStatus.Locked))
            .ToListAsync(ct);

        if (predictions.Count == 0)
            return;

        var spectatorIds = predictions.Select(p => p.SpectatorId).Distinct().ToList();
        var wallets = await _context.PointWallets
            .Where(w => spectatorIds.Contains(w.SpectatorId))
            .ToListAsync(ct);

        foreach (var prediction in predictions)
        {
            var wallet = wallets.FirstOrDefault(w => w.SpectatorId == prediction.SpectatorId);
            if (wallet is null || wallet.IsFrozen)
            {
                prediction.Status = PredictionStatus.Cancelled;
                prediction.CancelledAt = now;
                continue;
            }

            prediction.Status = PredictionStatus.Cancelled;
            prediction.CancelledAt = now;
            wallet.Balance += prediction.BetAmount;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = prediction.SpectatorId,
                PredictionId = prediction.PredictionId,
                Type = "BetRefund",
                Amount = prediction.BetAmount,
                BalanceAfter = wallet.Balance,
                Reason = WalletTransactionReasonBuilder.BetRefundHorseRevoked(
                prediction.Race!,
                prediction.FirstEntry!.Horse,
                prediction.FirstEntry.Jockey),
                CreatedAt = now
            });
        }
    }

}
