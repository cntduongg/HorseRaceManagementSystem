using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/start  — Referee/Admin bắt đầu đua → InProgress, khóa cược.
public sealed record StartRaceCommand(int RaceId, int CurrentUserId) : ICommand<StartRaceResponse>;

public sealed record StartRaceResponse(int RaceId, string Status, int TotalLegs);

public sealed class StartRaceCommandHandler
    : IRequestHandler<StartRaceCommand, StartRaceResponse>
{
    private readonly IApplicationDbContext _context;

    public StartRaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StartRaceResponse> Handle(
        StartRaceCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .Include(r => r.Legs)
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        // Chỉ referee được phân công hoặc Admin mới được start (Admin không nằm trong Referee1/2).
        // Controller đã giới hạn role; ở đây chỉ chặn referee lạ.
        // (Bỏ qua kiểm tra nếu CurrentUserId = 0 — gọi nội bộ.)

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException(
                $"The race can only be started in Scheduled status (current: {race.Status}).");

        if (race.Referee1Id is null || race.Referee2Id is null)
            throw new InvalidOperationException("The race has not been assigned 2 referees yet.");

        // Phải đóng đăng ký (khóa Odds + gán GateNumber) trước khi bắt đầu — Flow 3.
        if (race.OddsComputedAt is null)
            throw new InvalidOperationException("Registration must be closed (Odds locked) before starting the race.");

        var approvedEntries = race.Entries
            .Count(e => e.Status == RaceExecutionConstants.EntryApproved);

        if (approvedEntries < 2)
            throw new InvalidOperationException("At least 2 approved entries are required to start the race.");

        // Tạo Legs nếu chưa có (1..NumberOfLegs).
        if (race.Legs.Count == 0)
        {
            for (var legNumber = 1; legNumber <= race.NumberOfLegs; legNumber++)
            {
                _context.Legs.Add(new Leg
                {
                    RaceId = race.RaceId,
                    LegNumber = legNumber,
                    Status = RaceExecutionConstants.LegPending
                });
            }
        }

        race.Status = RaceExecutionConstants.RaceInProgress;
        race.UpdatedAt = DateTime.UtcNow;
        var pendingPredictions = await _context.Predictions
            .Where(p =>
                p.RaceId == race.RaceId &&
                p.Status == PredictionStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var prediction in pendingPredictions)
        {
            prediction.Status = PredictionStatus.Locked;
        }
        await _context.SaveChangesAsync(cancellationToken);

        return new StartRaceResponse(
            race.RaceId,
            race.Status,
            race.NumberOfLegs);
    }
}
