using Application.Common;
using Application.Common.Interfaces;
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

    public RejectViolationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RejectViolationResponse> Handle(
        RejectViolationCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Lý do từ chối là bắt buộc.");

        var violation = await _context.Violations
            .FirstOrDefaultAsync(v => v.ViolationId == request.ViolationId, cancellationToken)
            ?? throw new KeyNotFoundException("Violation not found.");

        if (violation.Status != "Pending")
            throw new InvalidOperationException("Vi phạm này đã được xử lý.");

        violation.Status = "Rejected";
        violation.Penalty = "None"; // từ chối → không áp phạt (UI không còn hiện "Cảnh cáo").
        violation.ReviewedByAdminId = request.AdminId;
        violation.ReviewedAt = DateTime.UtcNow;
        violation.AdminNote = request.Reason.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectViolationResponse(violation.ViolationId, violation.Status);
    }
}
