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
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var entry = await _context.Entries
                .FirstOrDefaultAsync(
                    x => x.EntryId == request.EntryId,
                    cancellationToken);

            if (entry is null)
            {
                return false;
            }

            if (entry.HorseOwnerId != request.HorseOwnerId)
            {
                throw new UnauthorizedAccessException("You do not have permission to withdraw this entry.");
            }

            if (!string.Equals(entry.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only a Pending entry can be withdrawn.");
            }

            var now = DateTime.UtcNow;

            entry.Status = "Withdrawn";
            entry.UpdatedAt = now;

            var invitations = await _context.JockeyInvitations
                .Where(x =>
                    x.RaceId == entry.RaceId &&
                    x.HorseId == entry.HorseId &&
                    (
                        x.Status == "Pending" ||
                        x.Status == "Confirmed"
                    ))
                .ToListAsync(cancellationToken);

            foreach (var invitation in invitations)
            {
                invitation.Status = "Cancelled";
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}   