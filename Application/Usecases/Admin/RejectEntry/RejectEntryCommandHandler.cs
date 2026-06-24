using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
namespace Application.Usecases.Admin.RejectEntry;

public sealed class RejectEntryCommandHandler
    : IRequestHandler<RejectEntryCommand, RejectEntryResponse>
{
    private readonly IApplicationDbContext _context;

    public RejectEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RejectEntryResponse> Handle(
        RejectEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Entries
            .FirstOrDefaultAsync(x => x.EntryId == request.EntryId, cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException("Entry not found");

        if (entry.Status != EntryStatus.Pending)
            throw new InvalidOperationException("Entry is not pending");

        entry.Status = EntryStatus.Rejected;
        entry.RejectionReason = request.Reason;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectEntryResponse(
            entry.EntryId,
            entry.Status,
            entry.RejectionReason
        );
    }
}