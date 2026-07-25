using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
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

        await RaceRefereeValidator.EnsureRefereesAsync(
            _context, request.Referee1Id, request.Referee2Id, cancellationToken);

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.TournamentId == request.TournamentId, cancellationToken);

        if (tournament is null)
            throw new KeyNotFoundException("Tournament not found.");

        var startUtc = request.ScheduledStartTime.ToUniversalTime();
        var endUtc = request.ScheduledEndTime.ToUniversalTime();

        // Giờ kết thúc phải sau giờ bắt đầu.
        if (endUtc <= startUtc)
            throw new InvalidOperationException("ScheduledEndTime must be after ScheduledStartTime.");

        // Cả giờ bắt đầu lẫn kết thúc phải nằm trong khoảng ngày của Tournament.
        var startDate = DateOnly.FromDateTime(request.ScheduledStartTime);
        var endDate = DateOnly.FromDateTime(request.ScheduledEndTime);
        if (startDate < tournament.StartDate || endDate > tournament.EndDate)
        {
            throw new InvalidOperationException(
                $"The time window must fall within the tournament's date range " +
                $"({tournament.StartDate:yyyy-MM-dd} - {tournament.EndDate:yyyy-MM-dd}).");
        }

        // Chống đè lịch (overlap) với race khác — cùng giải đấu hoặc trùng trọng tài.
        await RaceScheduleGuard.EnsureNoOverlapAsync(
            _context, request.TournamentId, excludeRaceId: null,
            startUtc, endUtc, request.Referee1Id, request.Referee2Id, cancellationToken);

        var race = new Race
        {
            TournamentId = request.TournamentId,
            Name = request.Name.Trim(),
            ScheduledStartTime = startUtc,
            ScheduledEndTime = endUtc,
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

        await RaceLegProvisioner.EnsureLegsExistAsync(_context, race, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return race.RaceId;
    }
}