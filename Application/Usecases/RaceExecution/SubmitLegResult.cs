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
    string Status,            // AwaitingSecondReferee | Matched | Conflicted
    int LegIndex,
    int LegNumber,
    string Message,
    bool IsRaceComplete,
    int? NextLegIndex);

public sealed class SubmitLegResultCommandHandler
    : IRequestHandler<SubmitLegResultCommand, SubmitLegResultResponse>
{
    private readonly IApplicationDbContext _context;

    public SubmitLegResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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
                $"Chỉ submit được khi đua đang InProgress (hiện tại: {race.Status}).");

        var isAssignedReferee =
            request.CurrentUserId == race.Referee1Id ||
            request.CurrentUserId == race.Referee2Id;
        if (!isAssignedReferee)
            throw new UnauthorizedAccessException("Chỉ trọng tài được phân công mới được submit.");

        var leg = race.Legs.FirstOrDefault(l => l.LegNumber == legNumber)
            ?? throw new KeyNotFoundException("Leg not found.");

        if (leg.Status is RaceExecutionConstants.LegConfirmed
                       or RaceExecutionConstants.LegConflicted
                       or RaceExecutionConstants.LegResolved)
            throw new InvalidOperationException($"Leg {legNumber} đã khóa ({leg.Status}).");

        // ── Validate payload so với entry đã duyệt ──
        var approvedEntryIds = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .Select(e => e.EntryId)
            .ToListAsync(cancellationToken);

        var submitted = request.Entries ?? new List<SubmitPositionItem>();
        var submittedIds = submitted.Select(x => x.EntryId).ToHashSet();

        if (submitted.Count == 0)
            throw new InvalidOperationException("Phải nhập kết quả cho các entry.");
        if (!submittedIds.SetEquals(approvedEntryIds))
            throw new InvalidOperationException("Danh sách entry không khớp với các entry đã duyệt của cuộc đua.");

        // Không trùng thứ hạng dương.
        var positiveRanks = submitted
            .Where(x => x.Position > 0)
            .Select(x => x.Position)
            .ToList();
        if (positiveRanks.Count != positiveRanks.Distinct().Count())
            throw new InvalidOperationException("Thứ hạng bị trùng — mỗi vị trí chỉ gán cho 1 entry.");

        // ── Chặn submit trùng (append-only, 1 referee/leg) ──
        var alreadyMine = await _context.LegRefereeEntries.AnyAsync(
            x => x.RaceId == request.RaceId &&
                 x.LegNumber == legNumber &&
                 x.RefereeUserId == request.CurrentUserId,
            cancellationToken);
        if (alreadyMine)
            throw new InvalidOperationException("Bạn đã submit kết quả cho leg này rồi.");

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
        if (opponentEntries.Count == 0)
        {
            leg.Status = RaceExecutionConstants.LegAwaitingSecondReferee;
            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitLegResultResponse(
                RaceExecutionConstants.LegAwaitingSecondReferee,
                request.LegIndex,
                legNumber,
                "Đã ghi nhận kết quả của bạn. Đang chờ trọng tài còn lại submit.",
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
                    LegPoints = RaceExecutionConstants.LegPointsFor(finishPosition, resultStatus),
                    ConfirmationType = RaceExecutionConstants.AutoMatched,
                    ConfirmedAt = now
                });
            }

            var (isComplete, nextLegIndex) = AdvanceRaceIfComplete(race, legNumber);
            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitLegResultResponse(
                "Matched",
                request.LegIndex,
                legNumber,
                isComplete
                    ? $"Leg {legNumber} khớp. Đã hoàn tất tất cả các leg — chờ công bố kết quả."
                    : $"Leg {legNumber} khớp hoàn toàn và đã được xác nhận.",
                isComplete,
                nextLegIndex);
        }

        // Lệch → Conflicted + Paused.
        leg.Status = RaceExecutionConstants.LegConflicted;
        leg.ConflictReportedAt = now;
        race.Status = RaceExecutionConstants.RacePaused;
        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitLegResultResponse(
            "Conflicted",
            request.LegIndex,
            legNumber,
            $"Phát hiện chênh lệch giữa 2 trọng tài ở Leg {legNumber}. Cuộc đua tạm dừng, chờ Admin xử lý.",
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
