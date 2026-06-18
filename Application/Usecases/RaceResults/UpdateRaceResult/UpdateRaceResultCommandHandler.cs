using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceResults.UpdateRaceResult;

public sealed class UpdateRaceResultCommandHandler
    : IRequestHandler<UpdateRaceResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateRaceResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateRaceResultCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.RaceResults
            .FirstOrDefaultAsync(x =>
                x.RaceId == request.RaceId &&
                x.EntryId == request.EntryId,
                cancellationToken);

        if (entity is null)
            return false;

        entity.TotalPoints = request.TotalPoints;
        entity.FinalPosition = request.FinalPosition;
        entity.IsRaceDQ = request.IsRaceDQ;
        entity.LegWinCount = request.LegWinCount;
        entity.LegTop3Count = request.LegTop3Count;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}