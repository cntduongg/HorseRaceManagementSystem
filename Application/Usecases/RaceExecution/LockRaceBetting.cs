using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/lock-betting — đóng sổ cược trước khi vào cuộc đua (Flow 7).
// Sau khi khóa: không đặt cược mới, không hủy cược, không sửa odds nữa — và ĐÂY là điều kiện
// bắt buộc để bấm Start Race. Mọi prediction Pending chuyển sang Locked ngay tại đây thay vì
// đợi tới lúc start, để "đã khóa" là một trạng thái quan sát được, không phải hệ quả phụ.
public sealed record LockRaceBettingCommand(int RaceId) : ICommand<LockRaceBettingResponse>;

public sealed record LockRaceBettingResponse(
    int RaceId,
    DateTime BettingLockedAt,
    int LockedPredictions,
    bool WasAlreadyLocked);

public sealed class LockRaceBettingCommandHandler
    : IRequestHandler<LockRaceBettingCommand, LockRaceBettingResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public LockRaceBettingCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<LockRaceBettingResponse> Handle(
        LockRaceBettingCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException(
                $"Betting can only be locked while the race is Scheduled (current: {race.Status}).");

        if (race.OddsComputedAt is null)
            throw new InvalidOperationException(
                "Registration must be closed before betting can be locked.");

        if (race.BettingLockedAt is not null)
        {
            return new LockRaceBettingResponse(
                race.RaceId, race.BettingLockedAt.Value, 0, WasAlreadyLocked: true);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var lockedCount = await RaceBettingLock.LockAsync(_context, race, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new LockRaceBettingResponse(race.RaceId, now, lockedCount, WasAlreadyLocked: false);
    }
}

/// <summary>
/// Khóa sổ cược của một race. Tách riêng vì có hai đường gọi tới: Admin bấm nút, và
/// worker auto-start (race tới giờ mà chưa ai bấm thì vẫn phải chạy được — xem
/// <see cref="RaceLifecycleCoordinator.StartRaceAsync"/>).
/// </summary>
public static class RaceBettingLock
{
    /// <returns>Số prediction chuyển từ Pending sang Locked.</returns>
    public static async Task<int> LockAsync(
        IApplicationDbContext context,
        Race race,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (race.BettingLockedAt is not null)
            return 0;

        race.BettingLockedAt = now;
        race.UpdatedAt = now;

        var pending = await context.Predictions
            .Where(p => p.RaceId == race.RaceId && p.Status == PredictionStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var prediction in pending)
            prediction.Status = PredictionStatus.Locked;

        return pending.Count;
    }
}
