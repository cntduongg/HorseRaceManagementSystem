using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.CreateRace;

public sealed class CreateRaceCommandHandler
    : IRequestHandler<CreateRaceCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateRaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateRaceCommand request,
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

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.TournamentId == request.TournamentId, cancellationToken);

        if (tournament is null)
            throw new KeyNotFoundException("Tournament not found.");

        var scheduledDate = DateOnly.FromDateTime(request.ScheduledStartTime);
        if (scheduledDate < tournament.StartDate || scheduledDate > tournament.EndDate)
        {
            throw new InvalidOperationException(
                $"ScheduledStartTime phải nằm trong khoảng ngày của Tournament " +
                $"({tournament.StartDate:yyyy-MM-dd} - {tournament.EndDate:yyyy-MM-dd}).");
        }

        var race = new Race
        {
            TournamentId = request.TournamentId,
            Name = request.Name.Trim(),
            ScheduledStartTime = request.ScheduledStartTime.ToUniversalTime(),
            NumberOfLegs = request.NumberOfLegs,
            MaxHorses = request.MaxHorses,
            RoundType = request.RoundType,
            Referee1Id = request.Referee1Id,
            Referee2Id = request.Referee2Id,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        _context.Races.Add(race);
        await _context.SaveChangesAsync(cancellationToken);

        return race.RaceId;
    }
}