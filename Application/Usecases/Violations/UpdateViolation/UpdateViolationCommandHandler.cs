using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.UpdateViolation;

public sealed class UpdateViolationCommandHandler
    : IRequestHandler<UpdateViolationCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public UpdateViolationCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<bool> Handle(
        UpdateViolationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ViolationId <= 0)
            throw new InvalidOperationException("ViolationId is invalid.");

        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (request.LegNumber <= 0)
            throw new InvalidOperationException("LegNumber is invalid.");

        if (request.EntryId <= 0)
            throw new InvalidOperationException("EntryId is invalid.");

        if (request.ReportedByRefereeId <= 0)
            throw new InvalidOperationException("ReportedByRefereeId is invalid.");

        if (request.ActorAdminId <= 0)
            throw new InvalidOperationException("ActorAdminId is invalid.");

        if (string.IsNullOrWhiteSpace(request.ViolationType))
            throw new InvalidOperationException("ViolationType is required.");

        if (string.IsNullOrWhiteSpace(request.Penalty))
            throw new InvalidOperationException("Penalty is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        var validPenalties = new[]
        {
            "Warning",
            "Demote",
            "DQ",
            "None" // vi phạm đã bị từ chối (Rejected) mang Penalty="None".
        };

        if (!validPenalties.Contains(request.Penalty.Trim()))
            throw new InvalidOperationException(
                "Penalty must be Warning, Demote, DQ or None.");

        var validStatuses = new[]
        {
            "Pending",
            "Approved",
            "Rejected"
        };

        if (!validStatuses.Contains(request.Status.Trim()))
            throw new InvalidOperationException(
                "Status must be Pending, Approved or Rejected.");

        var requestedStatus = request.Status.Trim();
        var requestedPenalty = request.Penalty.Trim();
        if (requestedStatus == "Approved" && requestedPenalty == "None")
            throw new InvalidOperationException(
                "An approved violation must use Warning, Demote or DQ.");

        if (requestedStatus == "Rejected" && requestedPenalty != "None")
            throw new InvalidOperationException(
                "A rejected violation must use penalty None.");

        var violation = await _context.Violations
         .FirstOrDefaultAsync(
             x => x.ViolationId == request.ViolationId,
             cancellationToken);

        if (violation is null)
            return false;

        // Giữ lại trạng thái/án phạt hiện tại để rollback standings nếu cần (Task 4).
        var oldStatus = violation.Status;
        var oldPenalty = violation.Penalty;
        var newStatus = requestedStatus;
        var newPenalty = requestedPenalty;
        var newNote = ReviewHistoryReason.Normalize(
            request.AdminNote,
            required: false,
            fieldName: "Admin note");
        var newType = request.ViolationType.Trim();
        var newDescription = request.Description?.Trim();

        // No-op: bỏ qua audit + timestamp churn nếu không có thay đổi thực sự.
        var isNoOp =
            violation.RaceId == request.RaceId &&
            violation.LegNumber == request.LegNumber &&
            violation.EntryId == request.EntryId &&
            violation.ReportedByRefereeId == request.ReportedByRefereeId &&
            string.Equals(violation.ViolationType, newType, StringComparison.Ordinal) &&
            string.Equals(violation.Description ?? "", newDescription ?? "", StringComparison.Ordinal) &&
            string.Equals(oldPenalty, newPenalty, StringComparison.Ordinal) &&
            string.Equals(oldStatus, newStatus, StringComparison.Ordinal) &&
            string.Equals(violation.AdminNote ?? "", newNote ?? "", StringComparison.Ordinal);

        if (isNoOp)
            return true;

        // Từ đây là thay đổi thực sự — nếu có ảnh hưởng standings (đổi status/penalty/leg/entry/race)
        // thì race liên quan không được phép đang Finished (đã publish).
        var willAffectStandings =
            !string.Equals(oldStatus, newStatus, StringComparison.Ordinal) ||
            !string.Equals(oldPenalty, newPenalty, StringComparison.Ordinal) ||
            violation.RaceId != request.RaceId ||
            violation.LegNumber != request.LegNumber ||
            violation.EntryId != request.EntryId;

        if (willAffectStandings)
        {
            var currentRace = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == violation.RaceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");
            if (currentRace.Status == RaceExecutionConstants.RaceFinished)
                throw new InvalidOperationException("Race already published — unpublish it first.");

            // RaceId đổi sang race khác → check luôn cả race đích.
            if (violation.RaceId != request.RaceId)
            {
                var targetRace = await _context.Races
                    .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                    ?? throw new KeyNotFoundException("Target race not found.");
                if (targetRace.Status == RaceExecutionConstants.RaceFinished)
                    throw new InvalidOperationException(
                        "Target race already published — unpublish it first.");
            }
        }



        var legExists = await _context.Legs
            .AnyAsync(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber,
                cancellationToken);

        if (!legExists)
            throw new InvalidOperationException("Leg does not exist.");

        var entryExists = await _context.Entries
            .AnyAsync(x =>
                x.EntryId == request.EntryId &&
                x.RaceId == request.RaceId,
                cancellationToken);

        if (!entryExists)
            throw new InvalidOperationException("Entry does not belong to the race.");

        var refereeExists = await _context.Users
            .AnyAsync(x =>
                x.UserId == request.ReportedByRefereeId,
                cancellationToken);

        if (!refereeExists)
            throw new InvalidOperationException("Reported referee does not exist.");

        var adminExists = await _context.Users
            .AnyAsync(x => x.UserId == request.ActorAdminId, cancellationToken);

        if (!adminExists)
            throw new InvalidOperationException("Reviewed admin does not exist.");

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var raceOrEntryChanged =
                violation.RaceId != request.RaceId ||
                violation.EntryId != request.EntryId;
            var legChanged = violation.LegNumber != request.LegNumber;
            var penaltyChanged = !string.Equals(
                oldPenalty,
                newPenalty,
                StringComparison.Ordinal);
            var oldPenaltyContextChanged =
                raceOrEntryChanged ||
                (oldPenalty == "Demote" && legChanged);
            var newPenaltyContextChanged =
                raceOrEntryChanged ||
                (newPenalty == "Demote" && legChanged);

            var mustReverseOldPenalty =
                oldStatus == "Approved" &&
                (newStatus != "Approved" ||
                 penaltyChanged ||
                 oldPenaltyContextChanged);
            var mustApplyNewPenalty =
                newStatus == "Approved" &&
                (oldStatus != "Approved" ||
                 penaltyChanged ||
                 newPenaltyContextChanged);

            var oldAffectedResults = mustReverseOldPenalty
                ? await LoadAffectedResultsAsync(
                    violation.RaceId,
                    violation.LegNumber,
                    violation.EntryId,
                    oldPenalty,
                    cancellationToken)
                : [];

            var newAffectedResults = mustApplyNewPenalty
                ? await LoadAffectedResultsAsync(
                    request.RaceId,
                    request.LegNumber,
                    request.EntryId,
                    newPenalty,
                    cancellationToken)
                : [];

            var affectedResults = oldAffectedResults
                .Concat(newAffectedResults)
                .DistinctBy(x => new { x.RaceId, x.LegNumber, x.EntryId })
                .ToList();

            var beforeData = ViolationAuditSnapshot.Serialize(
                violation,
                ViolationAuditSnapshot.Standings(affectedResults));

            // Sĩ số race — Leg Points tuyến tính theo số ngựa nên apply/reverse phải cùng mốc này.
            var fieldSize = await _context.Entries
                .CountAsync(e => e.RaceId == request.RaceId &&
                                 e.Status == RaceExecutionConstants.EntryApproved,
                    cancellationToken);

            if (mustReverseOldPenalty)
                ReverseAppliedPenalty(oldPenalty, oldAffectedResults, fieldSize);

            violation.RaceId = request.RaceId;
            violation.LegNumber = request.LegNumber;
            violation.EntryId = request.EntryId;
            violation.ReportedByRefereeId = request.ReportedByRefereeId;
            violation.ViolationType = newType;
            violation.Description = newDescription;
            violation.Penalty = newPenalty;
            violation.Status = newStatus;
            violation.ReviewedByAdminId =
                newStatus is "Approved" or "Rejected"
                    ? request.ActorAdminId
                    : null;
            violation.AdminNote = newNote;
            violation.ReviewedAt = newStatus is "Approved" or "Rejected" ? DateTime.UtcNow : null;

            if (mustApplyNewPenalty)
                ApplyPenalty(newPenalty, newAffectedResults, fieldSize);

            var action = penaltyChanged
                ? ReviewAction.PenaltyChanged
                : ReviewAction.Updated;

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Violation,
                    EntityId = violation.ViolationId,
                    Action = action,
                    Reason = newNote,
                    BeforeData = beforeData,
                    AfterData = ViolationAuditSnapshot.Serialize(
                        violation,
                        ViolationAuditSnapshot.Standings(affectedResults)),
                    AdminId = request.ActorAdminId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<LegOfficialResult>> LoadAffectedResultsAsync(
        int raceId,
        int legNumber,
        int entryId,
        string penalty,
        CancellationToken cancellationToken)
    {
        return penalty switch
        {
            "Demote" => await _context.LegOfficialResults
                .Where(o =>
                    o.RaceId == raceId &&
                    o.LegNumber == legNumber &&
                    o.EntryId == entryId)
                .ToListAsync(cancellationToken),

            "DQ" => await _context.LegOfficialResults
                .Where(o =>
                    o.RaceId == raceId &&
                    o.EntryId == entryId)
                .ToListAsync(cancellationToken),

            _ => []
        };
    }

    private static void ApplyPenalty(
        string penalty,
        IReadOnlyList<LegOfficialResult> affectedResults,
        int fieldSize)
    {
        switch (penalty)
        {
            case "Demote":
                var official = affectedResults.FirstOrDefault();
                if (official is
                    {
                        ResultStatus: RaceExecutionConstants.ResultFinished,
                        FinishPosition: not null
                    })
                {
                    official.FinishPosition += 1;
                    official.LegPoints = RaceExecutionConstants.LegPointsFor(
                        official.FinishPosition,
                        official.ResultStatus,
                        fieldSize);
                }
                break;

            case "DQ":
                foreach (var result in affectedResults)
                {
                    result.ResultStatus = RaceExecutionConstants.ResultDq;
                    result.FinishPosition = null;
                    result.LegPoints = 0;
                }
                break;
        }
    }

    // DQ cannot be reversed because the legacy model overwrote original positions.
    private static void ReverseAppliedPenalty(
        string penalty,
        IReadOnlyList<LegOfficialResult> affectedResults,
        int fieldSize)
    {
        switch (penalty)
        {
            case "Demote":
                var official = affectedResults.FirstOrDefault();
                if (official is
                    {
                        ResultStatus: RaceExecutionConstants.ResultFinished,
                        FinishPosition: > 1
                    })
                {
                    official.FinishPosition -= 1;
                    official.LegPoints = RaceExecutionConstants.LegPointsFor(
                        official.FinishPosition,
                        official.ResultStatus,
                        fieldSize);
                }
                break;

            case "DQ":
                throw new InvalidOperationException(
                    "Cannot automatically revert an applied DQ violation " +
                    "(the original position has been overwritten). " +
                    "Please adjust the leg result via the resolve/override flow " +
                    "instead of editing the violation.");
        }

    }
}
