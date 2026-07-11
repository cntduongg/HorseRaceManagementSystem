using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.ResubmitHorse;

public sealed class ResubmitHorseCommandHandler
    : IRequestHandler<ResubmitHorseCommand, ResubmitHorseResult>
{
    private readonly IApplicationDbContext _context;

    public ResubmitHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResubmitHorseResult> Handle(
        ResubmitHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            return new ResubmitHorseResult(false, ResubmitHorseError.NotFound);

        // Chốt quyền sở hữu — chỗ mà PUT/DELETE hiện tại đang thiếu.
        if (horse.OwnerId != request.OwnerId)
            return new ResubmitHorseResult(false, ResubmitHorseError.Forbidden);

        if (horse.Status != HorseStatus.Rejected)
            return new ResubmitHorseResult(false, ResubmitHorseError.InvalidStatus);

        horse.Status = HorseStatus.Pending;
        horse.RejectionReason = null;
        horse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResubmitHorseResult(true, ResubmitHorseError.None, horse.HorseId, horse.Status);
    }
}