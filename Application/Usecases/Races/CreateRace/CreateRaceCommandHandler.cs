using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.CreateRace;

public sealed class CreateRaceCommandHandler
    : IRequestHandler<CreateRaceCommand, int>
{
    // Khoảng đệm tối thiểu (phút) giữa 2 cuộc đua trong cùng một giải đấu.
    private const int MinGapBetweenRacesMinutes = 30;

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

        // Chống trùng/chồng lấn khung giờ với race khác trong CÙNG tournament.
        // Race không có thời lượng cố định nên coi mỗi race cần cách nhau tối thiểu
        // MinGapBetweenRacesMinutes phút để có thời gian vận hành. Bỏ qua race đã Cancelled.
        var scheduledUtc = request.ScheduledStartTime.ToUniversalTime();
        var siblingStartTimes = await _context.Races
            .Where(r => r.TournamentId == request.TournamentId && r.Status != "Cancelled")
            .Select(r => r.ScheduledStartTime)
            .ToListAsync(cancellationToken);

        if (siblingStartTimes.Any(t =>
                Math.Abs((t - scheduledUtc).TotalMinutes) < MinGapBetweenRacesMinutes))
        {
            throw new InvalidOperationException(
                $"Khung giờ này trùng hoặc chồng lấn với một cuộc đua khác trong cùng giải đấu. " +
                $"Hai cuộc đua phải cách nhau tối thiểu {MinGapBetweenRacesMinutes} phút.");
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