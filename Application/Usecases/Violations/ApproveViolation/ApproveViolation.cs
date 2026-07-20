using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.ApproveViolation;

// POST /api/admin/violations/{id}/approve — Admin duyệt & áp dụng ngay vào standings.
//  Warning  → chỉ ghi nhận.
//  Demote   → tụt 1 hạng ở leg liên quan (recompute Leg Points).
//  DQ       → Race DQ: 0 điểm toàn bộ leg của entry (→ xếp cuối); Publish set IsRaceDQ + 0 Prize.
public sealed record ApproveViolationCommand(
    int ViolationId,
    int AdminId,
    string? Penalty,
    string? AdminNote) : ICommand<ApproveViolationResponse>;

public sealed record ApproveViolationResponse(int ViolationId, string Penalty, string Status);

public sealed class ApproveViolationCommandHandler
    : IRequestHandler<ApproveViolationCommand, ApproveViolationResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public ApproveViolationCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<ApproveViolationResponse> Handle(
        ApproveViolationCommand request,
        CancellationToken cancellationToken)
    {
        var adminNote = ReviewHistoryReason.Normalize(
            request.AdminNote,
            required: false,
            fieldName: "Admin note");

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var violation = await _context.Violations
                .FirstOrDefaultAsync(v => v.ViolationId == request.ViolationId, cancellationToken)
                ?? throw new KeyNotFoundException("Violation not found.");

            if (violation.Status != "Pending")
                throw new InvalidOperationException("This violation has already been processed.");

            var penalty = string.IsNullOrWhiteSpace(request.Penalty)
                ? violation.Penalty
                : request.Penalty.Trim();
            if (penalty is not ("Warning" or "Demote" or "DQ"))
                throw new InvalidOperationException("Penalty must be Warning, Demote or DQ.");

            var now = DateTime.UtcNow;

            // Sĩ số race — Leg Points tuyến tính theo số ngựa nên phải tính lại đúng mốc này.
            var fieldSize = await _context.Entries
                .CountAsync(e => e.RaceId == violation.RaceId &&
                                 e.Status == RaceExecutionConstants.EntryApproved,
                    cancellationToken);

            List<LegOfficialResult> affectedOfficials;
            switch (penalty)
            {
                case "Demote":
                    affectedOfficials = await _context.LegOfficialResults
                        .Where(o =>
                            o.RaceId == violation.RaceId &&
                            o.LegNumber == violation.LegNumber &&
                            o.EntryId == violation.EntryId)
                        .ToListAsync(cancellationToken);
                    break;

                case "DQ":
                    affectedOfficials = await _context.LegOfficialResults
                        .Where(o =>
                            o.RaceId == violation.RaceId &&
                            o.EntryId == violation.EntryId)
                        .ToListAsync(cancellationToken);
                    break;

                default:
                    affectedOfficials = [];
                    break;
            }

            var beforeData = ViolationAuditSnapshot.Serialize(
                violation,
                ViolationAuditSnapshot.Standings(affectedOfficials));

            switch (penalty)
            {
                case "Demote":
                    var official = affectedOfficials.FirstOrDefault();
                    if (official is { ResultStatus: RaceExecutionConstants.ResultFinished, FinishPosition: not null })
                    {
                        official.FinishPosition += 1;
                        official.LegPoints = RaceExecutionConstants.LegPointsFor(
                            official.FinishPosition, official.ResultStatus, fieldSize);
                    }
                    break;

                case "DQ":
                    foreach (var o in affectedOfficials)
                    {
                        o.ResultStatus = RaceExecutionConstants.ResultDq;
                        o.FinishPosition = null;
                        o.LegPoints = 0;
                    }
                    break;

                    // Warning: không đổi standings.
            }

            violation.Status = "Approved";
            violation.Penalty = penalty;
            violation.ReviewedByAdminId = request.AdminId;
            violation.ReviewedAt = now;
            violation.AdminNote = adminNote;

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Violation,
                    EntityId = violation.ViolationId,
                    Action = ReviewAction.Approved,
                    Reason = violation.AdminNote,
                    BeforeData = beforeData,
                    AfterData = ViolationAuditSnapshot.Serialize(
                        violation,
                        ViolationAuditSnapshot.Standings(affectedOfficials)),
                    AdminId = request.AdminId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new ApproveViolationResponse(violation.ViolationId, penalty, violation.Status);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
