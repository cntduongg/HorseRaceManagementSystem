using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/legs/{legIndex}/submit
// Referee submit kết quả leg (append-only). Khi cả 2 đã submit → so khớp tự động.
public sealed record SubmitLegResultCommand(
    int RaceId,
    int LegIndex,
    int CurrentUserId,
    IReadOnlyList<SubmitPositionItem> Entries) : ICommand<SubmitLegResultResponse>;

public sealed record SubmitPositionItem(int EntryId, int Position);

public sealed record SubmitLegResultResponse(
    string Status, // AwaitingSecondReferee | Matched | Conflicted
    int LegIndex,
    int LegNumber,
    string Message,
    bool IsRaceComplete,
    int? NextLegIndex);

public sealed class SubmitLegResultCommandHandler
    : IRequestHandler<SubmitLegResultCommand, SubmitLegResultResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IRaceLiveChangeTracker _liveTracker;

    public SubmitLegResultCommandHandler(
        IApplicationDbContext context,
        IRaceLiveChangeTracker liveTracker)
    {
        _context = context;
        _liveTracker = liveTracker;
    }

    public async Task<SubmitLegResultResponse> Handle(
        SubmitLegResultCommand request,
        CancellationToken cancellationToken)
    {
        var legNumber = request.LegIndex + 1;

        var race = await _context.Races
                       .Include(r => r.Legs)
                       .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                   ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceInProgress)
            throw new InvalidOperationException(
                $"You can only submit while the race is InProgress (current: {race.Status}).");

        var isAssignedReferee =
            request.CurrentUserId == race.Referee1Id ||
            request.CurrentUserId == race.Referee2Id;
        if (!isAssignedReferee)
            throw new UnauthorizedAccessException("Only an assigned referee can submit.");

        var leg = race.Legs.FirstOrDefault(l => l.LegNumber == legNumber)
                  ?? throw new KeyNotFoundException("Leg not found.");

        if (leg.Status is RaceExecutionConstants.LegConfirmed
            or RaceExecutionConstants.LegConflicted
            or RaceExecutionConstants.LegResolved)
            throw new InvalidOperationException($"Leg {legNumber} is already locked ({leg.Status}).");

        // ── Validate payload so với entry đã duyệt ──
        var approvedEntryIds = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .Select(e => e.EntryId)
            .ToListAsync(cancellationToken);

        var submitted = request.Entries ?? new List<SubmitPositionItem>();
        var submittedIds = submitted.Select(x => x.EntryId).ToHashSet();

        if (submitted.Count == 0)
            throw new InvalidOperationException("You must enter results for the entries.");
        if (!submittedIds.SetEquals(approvedEntryIds))
            throw new InvalidOperationException("The entry list does not match the race's approved entries.");

        // Số ngựa thực đua của race — vừa là biên trên của thứ hạng, vừa là cơ sở tính Leg Points.
        var fieldSize = approvedEntryIds.Count;

        // Vị trí phải nằm trong 1..fieldSize, không trùng, và liên tục từ 1 (bỏ DNF/DQ).
        var positionError = RaceExecutionConstants.ValidatePositions(
            submitted.Select(x => x.Position).ToList(), fieldSize);
        if (positionError is not null)
            throw new InvalidOperationException(positionError);

        // ── Chặn submit trùng (append-only, 1 referee/leg) ──
        var alreadyMine = await _context.LegRefereeEntries.AnyAsync(
            x => x.RaceId == request.RaceId &&
                 x.LegNumber == legNumber &&
                 x.RefereeUserId == request.CurrentUserId,
            cancellationToken);
        if (alreadyMine)
            throw new InvalidOperationException("You have already submitted results for this leg.");

        var now = DateTime.UtcNow;

        // ── Append bản ghi blind của tôi ──
        foreach (var item in submitted)
        {
            var (finishPosition, resultStatus) =
                RaceExecutionConstants.DecodePosition(item.Position);

            _context.LegRefereeEntries.Add(new LegRefereeEntry
            {
                RaceId = request.RaceId,
                LegNumber = legNumber,
                EntryId = item.EntryId,
                RefereeUserId = request.CurrentUserId,
                FinishPosition = finishPosition,
                ResultStatus = resultStatus,
                SubmittedAt = now
            });
        }

        if (leg.StartedAt is null) leg.StartedAt = now;

        // ── Đối chiếu với referee kia ──
        var opponentId = request.CurrentUserId == race.Referee1Id
            ? race.Referee2Id
            : race.Referee1Id;

        var opponentEntries = await _context.LegRefereeEntries
            .Where(x => x.RaceId == request.RaceId &&
                        x.LegNumber == legNumber &&
                        x.RefereeUserId == opponentId)
            .ToListAsync(cancellationToken);

        // Referee kia chưa submit → chờ.
        // CỐ Ý KHÔNG MarkChanged ở nhánh này: đẩy push lúc này là báo cho mọi spectator
        // "một trọng tài vừa nộp" — đúng nghĩa kênh phụ thời-gian-thực trên Blind Double-Entry.
        // Snapshot lúc này cũng giống hệt bản cũ (chưa có LegOfficialResult) nên không mất gì.
        if (opponentEntries.Count == 0)
        {
            leg.Status = RaceExecutionConstants.LegAwaitingSecondReferee;
            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitLegResultResponse(
                RaceExecutionConstants.LegAwaitingSecondReferee,
                request.LegIndex,
                legNumber,
                "Your results have been recorded. Waiting for the other referee to submit.",
                IsRaceComplete: false,
                NextLegIndex: null);
        }

        // Cả hai đã submit → so khớp từng vị trí.
        var mineMap = submitted.ToDictionary(
            x => x.EntryId,
            x => RaceExecutionConstants.DecodePosition(x.Position));
        var oppMap = opponentEntries.ToDictionary(
            x => x.EntryId,
            x => (x.FinishPosition, x.ResultStatus));

        var matched = mineMap.Count == oppMap.Count && mineMap.All(kv =>
            oppMap.TryGetValue(kv.Key, out var opp) &&
            opp.FinishPosition == kv.Value.finishPosition &&
            opp.ResultStatus == kv.Value.resultStatus);

        if (matched)
        {
            leg.Status = RaceExecutionConstants.LegConfirmed;
            leg.ConfirmationType = RaceExecutionConstants.AutoMatched;
            leg.ConfirmedAt = now;
            leg.FinishedAt = now;

            foreach (var item in submitted)
            {
                var (finishPosition, resultStatus) =
                    RaceExecutionConstants.DecodePosition(item.Position);

                _context.LegOfficialResults.Add(new LegOfficialResult
                {
                    RaceId = request.RaceId,
                    LegNumber = legNumber,
                    EntryId = item.EntryId,
                    FinishPosition = finishPosition,
                    ResultStatus = resultStatus,
                    LegPoints = RaceExecutionConstants.LegPointsFor(
                        finishPosition, resultStatus, fieldSize, legNumber, race.NumberOfLegs),
                    ConfirmationType = RaceExecutionConstants.AutoMatched,
                    ConfirmedAt = now
                });
            }

            var (isComplete, nextLegIndex) = AdvanceRaceIfComplete(race, legNumber);
            await _context.SaveChangesAsync(cancellationToken);

            // Leg đã chốt → vị trí chính thức xuất hiện, đẩy cho spectator.
            _liveTracker.MarkChanged(request.RaceId);

            return new SubmitLegResultResponse(
                "Matched",
                request.LegIndex,
                legNumber,
                isComplete
                    ? $"Leg {legNumber} matches. All legs completed — awaiting result publication."
                    : $"Leg {legNumber} matches completely and has been confirmed.",
                isComplete,
                nextLegIndex);
        }

        // Lệch → Conflicted + Paused.
        leg.Status = RaceExecutionConstants.LegConflicted;
        leg.ConflictReportedAt = now;
        race.Status = RaceExecutionConstants.RacePaused;
        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        // Leg lệch → race Paused. Spectator thấy "Đang xem xét" (không lộ submission nào).
        _liveTracker.MarkChanged(request.RaceId);

        return new SubmitLegResultResponse(
            "Conflicted",
            request.LegIndex,
            legNumber,
            $"Discrepancy detected between the two referees at Leg {legNumber}. The race is paused, awaiting Admin resolution.",
            IsRaceComplete: false,
            NextLegIndex: null);
    }

    // Đặt race sang PendingResult nếu mọi leg đã Confirmed/Resolved; trả nextLegIndex còn mở.
    private static (bool isComplete, int? nextLegIndex) AdvanceRaceIfComplete(Race race, int justConfirmedLeg)
    {
        var openLeg = race.Legs
            .Where(l => l.LegNumber != justConfirmedLeg)
            .Where(l => l.Status != RaceExecutionConstants.LegConfirmed &&
                        l.Status != RaceExecutionConstants.LegResolved)
            .OrderBy(l => l.LegNumber)
            .FirstOrDefault();

        if (openLeg is null)
        {
            race.Status = RaceExecutionConstants.RacePendingResult;
            race.UpdatedAt = DateTime.UtcNow;
            return (true, null);
        }

        return (false, openLeg.LegNumber - 1);
    }
}