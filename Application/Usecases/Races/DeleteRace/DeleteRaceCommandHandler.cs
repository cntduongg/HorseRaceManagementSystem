using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.DeleteRace;

public sealed class DeleteRaceCommandHandler
    : IRequestHandler<DeleteRaceCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteRaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteRaceCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            return false;

        _context.Races.Remove(race);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}