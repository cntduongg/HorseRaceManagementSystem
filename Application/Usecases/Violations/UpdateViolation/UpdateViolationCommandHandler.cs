using Application.Common.Interfaces;
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
        var violation = await _context.Violations
            .FirstOrDefaultAsync(x => x.ViolationId == request.ViolationId, cancellationToken);

        if (violation is null)
            return false;

        violation.RaceId = request.RaceId;
        violation.LegNumber = request.LegNumber;
        violation.EntryId = request.EntryId;
        violation.ReportedByRefereeId = request.ReportedByRefereeId;
        violation.ViolationType = request.ViolationType;
        violation.Description = request.Description;
        violation.Penalty = request.Penalty;
        violation.Status = request.Status;
        violation.ReviewedByAdminId = request.ReviewedByAdminId;
        violation.AdminNote = request.AdminNote;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}