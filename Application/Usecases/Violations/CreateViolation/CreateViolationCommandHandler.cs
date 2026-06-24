using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

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
        if (string.IsNullOrWhiteSpace(request.ViolationType))
            throw new InvalidOperationException("ViolationType is required.");

        if (string.IsNullOrWhiteSpace(request.Penalty))
            throw new InvalidOperationException("Penalty is required.");

        var violation = new Violation
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            EntryId = request.EntryId,
            ReportedByRefereeId = request.ReportedByRefereeId,
            ViolationType = request.ViolationType.Trim(),
            Description = request.Description,
            Penalty = request.Penalty,
            Status = request.Status,
            ReviewedByAdminId = request.ReviewedByAdminId,
            AdminNote = request.AdminNote,
            CreatedAt = DateTime.UtcNow
        };

        _context.Violations.Add(violation);
        await _context.SaveChangesAsync(cancellationToken);

        return violation.ViolationId;
    }
}