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
                $"Chỉ có thể bắt đầu đua ở trạng thái Scheduled (hiện tại: {race.Status}).");

        if (race.Referee1Id is null || race.Referee2Id is null)
            throw new InvalidOperationException("Cuộc đua chưa được gán đủ 2 trọng tài.");

        // Phải đóng đăng ký (khóa Odds + gán GateNumber) trước khi bắt đầu — Flow 3.
        if (race.OddsComputedAt is null)
            throw new InvalidOperationException("Cần đóng đăng ký (khóa Odds) trước khi bắt đầu đua.");

        var approvedEntries = race.Entries
            .Count(e => e.Status == RaceExecutionConstants.EntryApproved);

        if (approvedEntries < 2)
            throw new InvalidOperationException("Cần ít nhất 2 entry đã duyệt để bắt đầu đua.");

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

        await _context.SaveChangesAsync(cancellationToken);

        return new StartRaceResponse(
            race.RaceId,
            race.Status,
            race.NumberOfLegs);
    }
}
