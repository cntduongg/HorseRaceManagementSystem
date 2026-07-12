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
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (race is null)
            return false;

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.TournamentId == request.TournamentId, cancellationToken);

        if (tournament is null)
            throw new KeyNotFoundException("Tournament not found.");

        var startUtc = request.ScheduledStartTime.ToUniversalTime();
        var endUtc = request.ScheduledEndTime.ToUniversalTime();

        // Giờ kết thúc phải sau giờ bắt đầu.
        if (endUtc <= startUtc)
            throw new InvalidOperationException("ScheduledEndTime phải sau ScheduledStartTime.");

        // Cả giờ bắt đầu lẫn kết thúc phải nằm trong khoảng ngày của Tournament.
        var startDate = DateOnly.FromDateTime(request.ScheduledStartTime);
        var endDate = DateOnly.FromDateTime(request.ScheduledEndTime);
        if (startDate < tournament.StartDate || endDate > tournament.EndDate)
        {
            throw new InvalidOperationException(
                $"Khung giờ phải nằm trong khoảng ngày của Tournament " +
                $"({tournament.StartDate:yyyy-MM-dd} - {tournament.EndDate:yyyy-MM-dd}).");
        }

        // Chống đè lịch (overlap) với race KHÁC — cùng giải đấu hoặc trùng trọng tài (loại chính nó ra).
        await RaceScheduleGuard.EnsureNoOverlapAsync(
            _context, request.TournamentId, excludeRaceId: request.RaceId,
            startUtc, endUtc, request.Referee1Id, request.Referee2Id, cancellationToken);

        // Khóa số Legs khi đua đã rời Scheduled (đang/đã chạy) — Flow 3.
        if (race.Status != "Scheduled" && request.NumberOfLegs != race.NumberOfLegs)
            throw new InvalidOperationException(
                "Không thể đổi số Legs sau khi cuộc đua đã bắt đầu.");

        race.TournamentId = request.TournamentId;
        race.Name = request.Name.Trim();
        race.ScheduledStartTime = startUtc;
        race.ScheduledEndTime = endUtc;
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