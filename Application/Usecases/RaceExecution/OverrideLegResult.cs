using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/legs/{legIndex}/override
// Admin resolve conflict: chốt kết quả chính thức (AdminOverride) + lý do bắt buộc, resume đua.
public sealed record OverrideLegResultCommand(
    int RaceId,
    int LegIndex,
    int AdminUserId,
    string? OverrideReason,
    IReadOnlyList<OverrideDecisionItem> Decisions) : ICommand<OverrideLegResultResponse>;

public sealed record OverrideDecisionItem(int EntryId, int OfficialPosition);

public sealed record OverrideLegResultResponse(
    int LegIndex,
    int LegNumber,
    string LegStatus,
    string RaceStatus,
    bool IsRaceComplete);

public sealed class OverrideLegResultCommandHandler
    : IRequestHandler<OverrideLegResultCommand, OverrideLegResultResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IRaceLiveChangeTracker _liveTracker;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public OverrideLegResultCommandHandler(
        IApplicationDbContext context,
        IRaceLiveChangeTracker liveTracker,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _liveTracker = liveTracker;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<OverrideLegResultResponse> Handle(
        OverrideLegResultCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason))
            throw new InvalidOperationException("An override reason is required.");

        var legNumber = request.LegIndex + 1;

        var race = await _context.Races
                       .Include(r => r.Legs)
                       .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                   ?? throw new KeyNotFoundException("Race not found.");

        var leg = race.Legs.FirstOrDefault(l => l.LegNumber == legNumber)
                  ?? throw new KeyNotFoundException("Leg not found.");

        if (leg.Status != RaceExecutionConstants.LegConflicted)
            throw new InvalidOperationException(
                $"Only a Conflicted leg can be resolved (current: {leg.Status}).");

        // Snapshot trước khi đổi — cho audit trail.
        var beforeSnapshot = new
        {
            leg.RaceId,
            leg.LegNumber,
            leg.Status,
            leg.ConfirmationType,
            leg.AdminOverrideReason
        };

        var approvedEntryIds = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .Select(e => e.EntryId)
            .ToListAsync(cancellationToken);

        var decisions = request.Decisions ?? new List<OverrideDecisionItem>();
        var decisionIds = decisions.Select(d => d.EntryId).ToHashSet();
        if (!decisionIds.SetEquals(approvedEntryIds))
            throw new InvalidOperationException("The decision does not match the approved entries.");

        // Số ngựa thực đua — biên trên của thứ hạng và cơ sở tính Leg Points.
        var fieldSize = approvedEntryIds.Count;

        var positionError = RaceExecutionConstants.ValidatePositions(
            decisions.Select(d => d.OfficialPosition).ToList(), fieldSize);
        if (positionError is not null)
            throw new InvalidOperationException(positionError);

        var now = DateTime.UtcNow;
        var reason = request.OverrideReason!.Trim();

        // Xóa official result cũ của leg (nếu có) rồi ghi lại theo quyết định Admin.
        var existing = await _context.LegOfficialResults
            .Where(o => o.RaceId == request.RaceId && o.LegNumber == legNumber)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _context.LegOfficialResults.RemoveRange(existing);

        foreach (var d in decisions)
        {
            var (finishPosition, resultStatus) =
                RaceExecutionConstants.DecodePosition(d.OfficialPosition);

            _context.LegOfficialResults.Add(new LegOfficialResult
            {
                RaceId = request.RaceId,
                LegNumber = legNumber,
                EntryId = d.EntryId,
                FinishPosition = finishPosition,
                ResultStatus = resultStatus,
                LegPoints = RaceExecutionConstants.LegPointsFor(
                    finishPosition, resultStatus, fieldSize, legNumber, race.NumberOfLegs),
                ConfirmationType = RaceExecutionConstants.AdminOverride,
                ConfirmedAt = now,
                ConfirmedByAdminId = request.AdminUserId,
                OverrideReason = reason
            });
        }

        leg.Status = RaceExecutionConstants.LegResolved;
        leg.ConfirmationType = RaceExecutionConstants.AdminOverride;
        leg.AdminOverrideReason = reason;
        leg.ConfirmedAt = now;
        leg.FinishedAt = now;

        // Resume: nếu còn leg mở → InProgress; hết → PendingResult.
        var openLeg = race.Legs
            .Where(l => l.LegNumber != legNumber)
            .Any(l => l.Status != RaceExecutionConstants.LegConfirmed &&
                      l.Status != RaceExecutionConstants.LegResolved);

        race.Status = openLeg
            ? RaceExecutionConstants.RaceInProgress
            : RaceExecutionConstants.RacePendingResult;
        race.UpdatedAt = now;

        // Snapshot sau khi đổi — cho audit trail.
        var afterSnapshot = new
        {
            leg.RaceId,
            leg.LegNumber,
            leg.Status,
            leg.ConfirmationType,
            leg.AdminOverrideReason,
            Decisions = decisions
        };

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.Leg,
                EntityId = request.RaceId,
                Action = ReviewAction.AdminOverride,
                Reason = reason,
                BeforeData = JsonSerializer.Serialize(beforeSnapshot),
                AfterData = JsonSerializer.Serialize(afterSnapshot),
                AdminId = request.AdminUserId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Admin đã resolve → leg có vị trí chính thức (AdminOverride) + race hết Paused.
        _liveTracker.MarkChanged(request.RaceId);

        return new OverrideLegResultResponse(
            request.LegIndex,
            legNumber,
            leg.Status,
            race.Status,
            IsRaceComplete: !openLeg);
    }
}