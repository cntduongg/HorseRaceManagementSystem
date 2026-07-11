using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Application.Usecases.Admin.RevokeHorse;

public class RevokeHorseCommandHandler
    : IRequestHandler<RevokeHorseCommand, RevokeHorseResponse>
{
    // Chỉ cho phép Revoke khi Race chưa bắt đầu (Scheduled) hoặc đã kết thúc hẳn (Finished/Cancelled).
    private static readonly string[] AllowedRaceStatusesForRevoke =
    {
        RaceStatus.Scheduled,
        RaceStatus.Finished,
        RaceStatus.Cancelled
    };

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
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required.");

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

            // Chặn nếu có Entry (chưa Cancelled) thuộc Race đang diễn ra (Ongoing hoặc bất kỳ
            // status nào ngoài Scheduled/Finished/Cancelled).
            var hasEntryInActiveRace = horse.Entries.Any(e =>
                e.Status != EntryStatus.Cancelled &&
                !AllowedRaceStatusesForRevoke.Contains(e.Race.Status));

            if (hasEntryInActiveRace)
            {
                throw new InvalidOperationException(
                    "Ngựa đang có Entry trong race đang diễn ra, không thể thu hồi.");
            }

            // 1. Revoke — trạng thái riêng, KHÔNG dùng Rejected
            horse.Status = HorseStatus.Revoked;
            horse.RejectionReason = request.Reason;
            horse.UpdatedAt = DateTime.UtcNow;

            // 2. Chỉ tự động Cancel Entry đang Pending.
            //    Entry Approved: KHÔNG tự huỷ ngầm, để Admin xử lý thủ công riêng.
            var cancelled = 0;
            var approvedEntriesNeedingReview = new List<int>();

            foreach (var entry in horse.Entries)
            {
                if (entry.Status == EntryStatus.Pending)
                {
                    entry.Status = EntryStatus.Cancelled;
                    entry.UpdatedAt = DateTime.UtcNow;
                    cancelled++;
                }
                else if (entry.Status == EntryStatus.Approved)
                {
                    approvedEntriesNeedingReview.Add(entry.EntryId);
                }
            }

            // 3. Audit log
            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Horse,
                    EntityId = horse.HorseId,
                    Action = ReviewAction.Revoked,
                    Reason = request.Reason,
                    AdminId = request.AdminId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RevokeHorseResponse(
                horse.HorseId,
                horse.Status,
                cancelled,
                approvedEntriesNeedingReview
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}