using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.UpdateRace;

public sealed class UpdateRaceCommandHandler
    : IRequestHandler<UpdateRaceCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateRaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateRaceCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Race name is required.");

        if (request.NumberOfLegs < 1 || request.NumberOfLegs > 10)
            throw new InvalidOperationException("NumberOfLegs must be between 1 and 10.");

        if (request.Referee1Id <= 0)
            throw new InvalidOperationException("Referee1Id is required.");

        if (request.Referee2Id <= 0)
            throw new InvalidOperationException("Referee2Id is required.");

        if (request.Referee1Id == request.Referee2Id)
            throw new InvalidOperationException("Referees must be different.");

        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            return false;

        race.TournamentId = request.TournamentId;
        race.Name = request.Name.Trim();
        race.ScheduledStartTime = request.ScheduledStartTime;
        race.NumberOfLegs = request.NumberOfLegs;
        race.MaxHorses = request.MaxHorses;
        race.RoundType = request.RoundType;
        race.Referee1Id = request.Referee1Id;
        race.Referee2Id = request.Referee2Id;
        race.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}