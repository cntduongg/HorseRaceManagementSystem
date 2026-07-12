using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.UpdateRace;

public sealed class UpdateRaceCommandHandler
    : IRequestHandler<UpdateRaceCommand, bool>
{
    // Khoảng đệm tối thiểu (phút) giữa 2 cuộc đua trong cùng một giải đấu.
    private const int MinGapBetweenRacesMinutes = 30;

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

        var scheduledDate = DateOnly.FromDateTime(request.ScheduledStartTime);
        if (scheduledDate < tournament.StartDate || scheduledDate > tournament.EndDate)
        {
            throw new InvalidOperationException(
                $"ScheduledStartTime phải nằm trong khoảng ngày của Tournament " +
                $"({tournament.StartDate:yyyy-MM-dd} - {tournament.EndDate:yyyy-MM-dd}).");
        }

        // Chống trùng/chồng lấn khung giờ với race KHÁC trong CÙNG tournament (loại chính nó ra).
        // Race không có thời lượng cố định nên coi mỗi race cần cách nhau tối thiểu
        // MinGapBetweenRacesMinutes phút để có thời gian vận hành. Bỏ qua race đã Cancelled.
        var scheduledUtc = request.ScheduledStartTime.ToUniversalTime();
        var siblingStartTimes = await _context.Races
            .Where(r => r.TournamentId == request.TournamentId
                        && r.RaceId != request.RaceId
                        && r.Status != "Cancelled")
            .Select(r => r.ScheduledStartTime)
            .ToListAsync(cancellationToken);

        if (siblingStartTimes.Any(t =>
                Math.Abs((t - scheduledUtc).TotalMinutes) < MinGapBetweenRacesMinutes))
        {
            throw new InvalidOperationException(
                $"Khung giờ này trùng hoặc chồng lấn với một cuộc đua khác trong cùng giải đấu. " +
                $"Hai cuộc đua phải cách nhau tối thiểu {MinGapBetweenRacesMinutes} phút.");
        }

        // Khóa số Legs khi đua đã rời Scheduled (đang/đã chạy) — Flow 3.
        if (race.Status != "Scheduled" && request.NumberOfLegs != race.NumberOfLegs)
            throw new InvalidOperationException(
                "Không thể đổi số Legs sau khi cuộc đua đã bắt đầu.");

        race.TournamentId = request.TournamentId;
        race.Name = request.Name.Trim();
        race.ScheduledStartTime = request.ScheduledStartTime.ToUniversalTime();
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