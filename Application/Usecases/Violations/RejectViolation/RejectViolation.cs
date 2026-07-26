using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.RejectViolation;

// POST /api/admin/violations/{id}/reject — Admin từ chối (lý do bắt buộc).
public sealed record RejectViolationCommand(
    int ViolationId,
    int AdminId,
    string Reason) : ICommand<RejectViolationResponse>;

public sealed record RejectViolationResponse(int ViolationId, string Status);

public sealed class RejectViolationCommandHandler
    : IRequestHandler<RejectViolationCommand, RejectViolationResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public RejectViolationCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<RejectViolationResponse> Handle(
        RejectViolationCommand request,
        CancellationToken cancellationToken)
    {
        var reason = ReviewHistoryReason.Normalize(
            request.Reason,
            required: true,
            fieldName: "Rejection reason")!;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var violation = await _context.Violations
                .FirstOrDefaultAsync(v => v.ViolationId == request.ViolationId, cancellationToken)
                ?? throw new KeyNotFoundException("Violation not found.");

            var race = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == violation.RaceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");
            if (race.Status == RaceExecutionConstants.RaceFinished)
                throw new InvalidOperationException("Race already published — unpublish it first.");

            if (violation.Status != "Pending")
                throw new InvalidOperationException("This violation has already been processed.");

            var beforeData = ViolationAuditSnapshot.Serialize(violation);

            violation.Status = "Rejected";
            violation.Penalty = "None"; // từ chối → không áp phạt (UI không còn hiện "Cảnh cáo").
            violation.ReviewedByAdminId = request.AdminId;
            violation.ReviewedAt = DateTime.UtcNow;
            violation.AdminNote = reason;

            await _reviewHistoryRepository.AddAsync(
                new ReviewHistory
                {
                    EntityType = ReviewEntity.Violation,
                    EntityId = violation.ViolationId,
                    Action = ReviewAction.Rejected,
                    Reason = reason,
                    BeforeData = beforeData,
                    AfterData = ViolationAuditSnapshot.Serialize(violation),
                    AdminId = request.AdminId
                },
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new RejectViolationResponse(violation.ViolationId, violation.Status);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}