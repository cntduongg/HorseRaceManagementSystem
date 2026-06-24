using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
namespace Application.Usecases.Admin.ApproveEntry;

public sealed class ApproveEntryCommandHandler
    : IRequestHandler<ApproveEntryCommand, ApproveEntryResponse>
{
    private readonly IApplicationDbContext _context;

    public ApproveEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApproveEntryResponse> Handle(
        ApproveEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Entries
            .FirstOrDefaultAsync(x => x.EntryId == request.EntryId, cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException("Entry not found");

        if (entry.Status != EntryStatus.Pending)
            throw new InvalidOperationException("Entry is not pending");

        // OPTIONAL: assign gate number
        var usedGates = await _context.Entries
            .Where(e => e.RaceId == entry.RaceId && e.GateNumber != null)
            .Select(e => e.GateNumber!.Value)
            .ToListAsync(cancellationToken);

        int nextGate = 1;
        while (usedGates.Contains(nextGate))
            nextGate++;

        entry.Status = EntryStatus.Approved;
        entry.ApprovedAt = DateTime.UtcNow;
        entry.ApprovedBy = request.AdminId;
        entry.GateNumber = nextGate;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveEntryResponse(
            entry.EntryId,
            entry.Status,
            entry.GateNumber
        );
    }
}