using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Microsoft.EntityFrameworkCore;
using MediatR;
namespace Application.Usecases.Admin.RejectHorse;

public class RejectHorseCommandHandler
    : IRequestHandler<RejectHorseCommand, RejectHorseResponse>
{
    private readonly IApplicationDbContext _context;

    public RejectHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RejectHorseResponse> Handle(
        RejectHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            throw new KeyNotFoundException("Horse not found");

        if (horse.Status != HorseStatus.Pending)
            throw new InvalidOperationException("Horse is not pending");

        horse.Status = HorseStatus.Rejected;
        horse.RejectionReason = request.Reason;
        horse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectHorseResponse(
            horse.HorseId,
            horse.Status,
            horse.RejectionReason
        );
    }
}