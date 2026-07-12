using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.UpdateViolation;

public sealed class UpdateViolationCommandHandler
    : IRequestHandler<UpdateViolationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateViolationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        var violation = await _context.Violations
            .FirstOrDefaultAsync(
                x => x.ViolationId == request.ViolationId,
                cancellationToken);

        if (violation is null)
            return false;

        // Giữ lại trạng thái/án phạt hiện tại để rollback standings nếu cần (Task 4).
        var oldStatus = violation.Status;
        var oldPenalty = violation.Penalty;
        var newStatus = request.Status.Trim();

        var legExists = await _context.Legs
            .AnyAsync(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber,
                cancellationToken);

        if (!legExists)
            throw new InvalidOperationException("Leg does not exist.");

        var entryExists = await _context.Entries
            .AnyAsync(x =>
                x.EntryId == request.EntryId,
                cancellationToken);

        if (!entryExists)
            throw new InvalidOperationException("Entry does not exist.");

        var refereeExists = await _context.Users
            .AnyAsync(x =>
                x.UserId == request.ReportedByRefereeId,
                cancellationToken);

        if (!refereeExists)
            throw new InvalidOperationException("Reported referee does not exist.");

        if (request.ReviewedByAdminId.HasValue)
        {
            var adminExists = await _context.Users
                .AnyAsync(x =>
                    x.UserId == request.ReviewedByAdminId.Value,
                    cancellationToken);

            if (!adminExists)
                throw new InvalidOperationException("Reviewed admin does not exist.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Rollback standings khi một vi phạm ĐÃ Approved bị chuyển khỏi Approved
            // (vd Approved → Pending/Rejected qua modal Sửa). Đảo ngược đúng án phạt cũ.
            if (oldStatus == "Approved" && newStatus != "Approved")
            {
                await ReverseAppliedPenaltyAsync(violation, oldPenalty, cancellationToken);
            }

            violation.RaceId = request.RaceId;
            violation.LegNumber = request.LegNumber;
            violation.EntryId = request.EntryId;
            violation.ReportedByRefereeId = request.ReportedByRefereeId;
            violation.ViolationType = request.ViolationType.Trim();
            violation.Description = request.Description?.Trim();
            violation.Penalty = request.Penalty.Trim();
            violation.Status = newStatus;
            violation.ReviewedByAdminId = request.ReviewedByAdminId;
            violation.AdminNote = request.AdminNote?.Trim();
            violation.ReviewedAt = newStatus is "Approved" or "Rejected" ? DateTime.UtcNow : null;

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

    // Đảo ngược tác động lên standings của một án phạt đã áp (mirror của ApproveViolation).
    //  - Demote: FinishPosition -= 1 rồi recompute Leg Points (đảo ngược chính xác).
    //  - DQ: ApproveViolation ghi đè FinishPosition = null (mất vị trí gốc) nên KHÔNG thể
    //        khôi phục tự động → chặn thao tác để tránh làm sai standings âm thầm.
    //  - Warning/None: không đụng standings.
    private async Task ReverseAppliedPenaltyAsync(
        Domain.Aggregates.Entities.Violation violation,
        string oldPenalty,
        CancellationToken cancellationToken)
    {
        switch (oldPenalty)
        {
            case "Demote":
                var official = await _context.LegOfficialResults.FirstOrDefaultAsync(
                    o => o.RaceId == violation.RaceId &&
                         o.LegNumber == violation.LegNumber &&
                         o.EntryId == violation.EntryId,
                    cancellationToken);
                if (official is { ResultStatus: RaceExecutionConstants.ResultFinished, FinishPosition: > 1 })
                {
                    official.FinishPosition -= 1;
                    official.LegPoints = RaceExecutionConstants.LegPointsFor(
                        official.FinishPosition, official.ResultStatus);
                }
                break;

            case "DQ":
                throw new InvalidOperationException(
                    "Cannot automatically revert an applied DQ violation (the original position has been overwritten). " +
                    "Please adjust the leg result via the resolve/override flow instead of editing the violation.");

                // Warning / None: không có tác động standings để hoàn tác.
        }
    }
}