using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.Discrepancies;

// POST /api/admin/discrepancies/{id}/resolve
public sealed record ResolveDiscrepancyCommand(
    int DiscrepancyId,
    string Resolution,
    string Action,                 // Dismissed | AdjustPoints
    int AdjustedPointsAwarded,
    int AdminId) : IRequest<ResolveDiscrepancyResponse>;

public sealed record ResolveDiscrepancyResponse(int DiscrepancyId, string Status);

public sealed class ResolveDiscrepancyCommandHandler
    : IRequestHandler<ResolveDiscrepancyCommand, ResolveDiscrepancyResponse>
{
    private readonly IApplicationDbContext _context;

    public ResolveDiscrepancyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResolveDiscrepancyResponse> Handle(
        ResolveDiscrepancyCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Resolution))
            throw new InvalidOperationException("A resolution note is required.");

        var discrepancy = await _context.Discrepancies
            .FirstOrDefaultAsync(d => d.DiscrepancyId == request.DiscrepancyId, cancellationToken)
            ?? throw new KeyNotFoundException("Discrepancy not found.");

        if (discrepancy.Status != "Pending")
            throw new InvalidOperationException("This discrepancy has already been resolved.");

        var status = request.Action == "AdjustPoints" ? "Resolved" : "Dismissed";

        discrepancy.Status = status;
        discrepancy.ResolutionAction = request.Action;
        discrepancy.Resolution = request.Resolution.Trim();
        discrepancy.AdjustedPointsAwarded =
            request.Action == "AdjustPoints" ? request.AdjustedPointsAwarded : 0;
        discrepancy.ResolvedByAdminId = request.AdminId;
        discrepancy.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResolveDiscrepancyResponse(discrepancy.DiscrepancyId, status);
    }
}
