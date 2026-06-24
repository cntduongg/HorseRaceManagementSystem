using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Microsoft.EntityFrameworkCore;
using MediatR;
namespace Application.Usecases.Admin.RevokeHorse;

public class RevokeHorseCommandHandler
    : IRequestHandler<RevokeHorseCommand, RevokeHorseResponse>
{
    private readonly IApplicationDbContext _context;

    public RevokeHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RevokeHorseResponse> Handle(
        RevokeHorseCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var horse = await _context.Horses
                .Include(h => h.Entries)
                .FirstOrDefaultAsync(h => h.HorseId == request.HorseId, cancellationToken);

            if (horse is null)
                throw new KeyNotFoundException("Horse not found");

            if (horse.Status != HorseStatus.Approved)
                throw new InvalidOperationException("Only approved horse can be revoked");

            // 1. revoke horse
            horse.Status = HorseStatus.Rejected;
            horse.UpdatedAt = DateTime.UtcNow;

            // 2. cancel entries
            var cancelled = 0;

            foreach (var entry in horse.Entries)
            {
                if (entry.Status != EntryStatus.Cancelled)
                {
                    entry.Status = EntryStatus.Cancelled;
                    entry.UpdatedAt = DateTime.UtcNow;
                    cancelled++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RevokeHorseResponse(
                horse.HorseId,
                horse.Status,
                cancelled
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}