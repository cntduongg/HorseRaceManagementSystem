using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.CreateViolation;

public sealed class CreateViolationCommandHandler
    : IRequestHandler<CreateViolationCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateViolationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateViolationCommand request,
        CancellationToken cancellationToken)
    {
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

        var validPenalties = new[] { "Warning", "Demote", "DQ" };

        if (!validPenalties.Contains(request.Penalty.Trim()))
            throw new InvalidOperationException(
                "Penalty must be Warning, Demote or DQ.");

        var validStatuses = new[] { "Pending", "Approved", "Rejected" };

        if (!validStatuses.Contains(request.Status.Trim()))
            throw new InvalidOperationException(
                "Status must be Pending, Approved or Rejected.");

        var legExists = await _context.Legs.AnyAsync(x =>
            x.RaceId == request.RaceId &&
            x.LegNumber == request.LegNumber,
            cancellationToken);

        if (!legExists)
            throw new InvalidOperationException("Leg does not exist.");

        var entryExists = await _context.Entries.AnyAsync(x =>
            x.EntryId == request.EntryId,
            cancellationToken);

        if (!entryExists)
            throw new InvalidOperationException("Entry does not exist.");

        var refereeExists = await _context.Users.AnyAsync(x =>
            x.UserId == request.ReportedByRefereeId,
            cancellationToken);

        if (!refereeExists)
            throw new InvalidOperationException("Reported referee does not exist.");

        if (request.ReviewedByAdminId.HasValue)
        {
            var adminExists = await _context.Users.AnyAsync(x =>
                x.UserId == request.ReviewedByAdminId.Value,
                cancellationToken);

            if (!adminExists)
                throw new InvalidOperationException("Reviewed admin does not exist.");
        }

        var violation = new Violation
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            EntryId = request.EntryId,
            ReportedByRefereeId = request.ReportedByRefereeId,
            ViolationType = request.ViolationType.Trim(),
            Description = request.Description?.Trim(),
            Penalty = request.Penalty.Trim(),
            Status = request.Status.Trim(),
            ReviewedByAdminId = request.ReviewedByAdminId,
            AdminNote = request.AdminNote?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Violations.Add(violation);

        await _context.SaveChangesAsync(cancellationToken);

        return violation.ViolationId;
    }
}