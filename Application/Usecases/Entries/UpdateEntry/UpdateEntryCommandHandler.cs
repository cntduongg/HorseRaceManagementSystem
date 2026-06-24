using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.UpdateEntry;

public sealed class UpdateEntryCommandHandler
    : IRequestHandler<UpdateEntryCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId,
                cancellationToken);

        if (entry is null)
            return false;

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        entry.Status = request.Status;

        if (request.GateNumber.HasValue)
            entry.GateNumber = request.GateNumber;

        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}