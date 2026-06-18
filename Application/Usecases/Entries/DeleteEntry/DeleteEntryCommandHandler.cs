using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.DeleteEntry;

public sealed class DeleteEntryCommandHandler
    : IRequestHandler<DeleteEntryCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId,
                cancellationToken);

        if (entry is null)
            return false;

        _context.Entries.Remove(entry);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}