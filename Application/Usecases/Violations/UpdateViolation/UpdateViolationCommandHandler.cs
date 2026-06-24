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
            "DQ"
        };

        if (!validPenalties.Contains(request.Penalty.Trim()))
            throw new InvalidOperationException(
                "Penalty must be Warning, Demote or DQ.");

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

        violation.RaceId = request.RaceId;
        violation.LegNumber = request.LegNumber;
        violation.EntryId = request.EntryId;
        violation.ReportedByRefereeId = request.ReportedByRefereeId;
        violation.ViolationType = request.ViolationType.Trim();
        violation.Description = request.Description?.Trim();
        violation.Penalty = request.Penalty.Trim();
        violation.Status = request.Status.Trim();
        violation.ReviewedByAdminId = request.ReviewedByAdminId;
        violation.AdminNote = request.AdminNote?.Trim();

        if (request.Status == "Approved" || request.Status == "Rejected")
        {
            violation.ReviewedAt = DateTime.UtcNow;
        }
        else
        {
            violation.ReviewedAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}