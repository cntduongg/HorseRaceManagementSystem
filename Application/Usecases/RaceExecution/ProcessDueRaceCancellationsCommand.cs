using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Quét và auto-cancel Race Scheduled đã quá hạn:
///  - Đã qua ScheduledStartTime mà số Entry Pending/Approved &lt; 2 (không đủ điều kiện start), HOẶC
///  - Đã qua ScheduledEndTime (dù đủ Entry nhưng chưa từng được start vì lý do khác).
/// Cascade dọn dẹp Entry/JockeyInvitation liên quan qua CancelRaceAsync (dùng chung với Delete tay).
/// Idempotent; một Race lỗi không chặn các Race khác.
/// </summary>
public sealed record ProcessDueRaceCancellationsCommand : ICommand<ProcessDueRaceCancellationsResponse>;

public sealed record DueRaceCancellationItem(
    int RaceId,
    RaceCancelOutcome Outcome,
    string? SkipReason);

public sealed record ProcessDueRaceCancellationsResponse(
    int Examined,
    int Cancelled,
    int Skipped,
    IReadOnlyList<DueRaceCancellationItem> Items);

public sealed class ProcessDueRaceCancellationsCommandHandler
    : IRequestHandler<ProcessDueRaceCancellationsCommand, ProcessDueRaceCancellationsResponse>
{
    private const int BatchSize = 50;
    private const int MinEntriesToStart = 2;

    private readonly IApplicationDbContext _context;
    private readonly IRaceLifecycleCoordinator _lifecycle;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessDueRaceCancellationsCommandHandler> _logger;

    public ProcessDueRaceCancellationsCommandHandler(
        IApplicationDbContext context,
        IRaceLifecycleCoordinator lifecycle,
        TimeProvider timeProvider,
        ILogger<ProcessDueRaceCancellationsCommandHandler> logger)
    {
        _context = context;
        _lifecycle = lifecycle;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ProcessDueRaceCancellationsResponse> Handle(
        ProcessDueRaceCancellationsCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var dueRaceIds = await _context.Races
            .AsNoTracking()
            .Where(r =>
                r.Status == RaceExecutionConstants.RaceScheduled &&
                (
                    r.ScheduledEndTime <= now ||
                    (
                        r.ScheduledStartTime <= now &&
                        r.Entries.Count(e =>
                            e.Status == EntryStatus.Pending ||
                            e.Status == EntryStatus.Approved) < MinEntriesToStart
                    )
                ))
            .OrderBy(r => r.ScheduledStartTime)
            .Select(r => r.RaceId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var items = new List<DueRaceCancellationItem>(dueRaceIds.Count);
        var cancelled = 0;
        var skipped = 0;

        foreach (var raceId in dueRaceIds)
        {
            try
            {
                var result = await _lifecycle.CancelRaceAsync(
                    raceId,
                    reason: "Race automatically cancelled: past its scheduled time without enough approved entries.",
                    throwOnFailure: false,
                    cancellationToken);

                items.Add(new DueRaceCancellationItem(raceId, result.Outcome, result.SkipReason));

                if (result.Outcome == RaceCancelOutcome.Cancelled)
                {
                    cancelled++;
                    _logger.LogInformation(
                        "Auto-cancelled race {RaceId} (withdrawnEntries={Entries}, cancelledInvitations={Invitations}).",
                        raceId, result.WithdrawnEntries, result.CancelledInvitations);
                }
                else
                {
                    skipped++;
                    _logger.LogWarning(
                        "Skipped auto-cancel for race {RaceId}: {Reason}",
                        raceId, result.SkipReason);
                }
            }
            catch (Exception ex)
            {
                skipped++;
                items.Add(new DueRaceCancellationItem(raceId, RaceCancelOutcome.Skipped, ex.Message));
                _logger.LogError(ex, "Failed auto-cancel for race {RaceId}.", raceId);
            }
        }

        return new ProcessDueRaceCancellationsResponse(
            dueRaceIds.Count,
            cancelled,
            skipped,
            items);
    }
}