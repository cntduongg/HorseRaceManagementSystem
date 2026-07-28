using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Quét và auto-start mọi Race Scheduled đã tới ScheduledStartTime.
/// Idempotent; một Race lỗi không chặn các Race khác.
/// </summary>
public sealed record ProcessDueRaceStartsCommand : ICommand<ProcessDueRaceStartsResponse>;

public sealed record DueRaceStartItem(
    int RaceId,
    RaceStartOutcome Outcome,
    string? SkipReason);

public sealed record ProcessDueRaceStartsResponse(
    int Examined,
    int Started,
    int AlreadyStarted,
    int Skipped,
    IReadOnlyList<DueRaceStartItem> Items);

public sealed class ProcessDueRaceStartsCommandHandler
    : IRequestHandler<ProcessDueRaceStartsCommand, ProcessDueRaceStartsResponse>
{
    private const int BatchSize = 50;

    private readonly IApplicationDbContext _context;
    private readonly IRaceLifecycleCoordinator _lifecycle;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessDueRaceStartsCommandHandler> _logger;

    public ProcessDueRaceStartsCommandHandler(
        IApplicationDbContext context,
        IRaceLifecycleCoordinator lifecycle,
        TimeProvider timeProvider,
        ILogger<ProcessDueRaceStartsCommandHandler> logger)
    {
        _context = context;
        _lifecycle = lifecycle;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ProcessDueRaceStartsResponse> Handle(
        ProcessDueRaceStartsCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var dueRaceIds = await _context.Races
            .AsNoTracking()
            .Where(r =>
                r.Status == RaceExecutionConstants.RaceScheduled &&
                r.ScheduledStartTime <= now)
            .OrderBy(r => r.ScheduledStartTime)
            .Select(r => r.RaceId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var items = new List<DueRaceStartItem>(dueRaceIds.Count);
        var started = 0;
        var already = 0;
        var skipped = 0;

        foreach (var raceId in dueRaceIds)
        {
            try
            {
                var result = await _lifecycle.StartRaceAsync(
                    raceId,
                    enforceSchedule: true,
                    allowAutoClose: true,
                    throwOnFailure: false,
                    // Worker tự khóa sổ cược: race tới giờ mà Admin quên bấm "Lock Betting"
                    // thì vẫn phải chạy, không được kẹt ở Scheduled vĩnh viễn.
                    requireBettingLocked: false,
                    cancellationToken);

                items.Add(new DueRaceStartItem(raceId, result.Outcome, result.SkipReason));

                switch (result.Outcome)
                {
                    case RaceStartOutcome.Started:
                        started++;
                        _logger.LogInformation(
                            "Auto-started race {RaceId} (registrationClosed={Closed}).",
                            raceId, result.RegistrationWasClosed);
                        break;
                    case RaceStartOutcome.AlreadyStarted:
                        already++;
                        break;
                    case RaceStartOutcome.Skipped:
                        skipped++;
                        _logger.LogWarning(
                            "Skipped auto-start for race {RaceId}: {Reason}",
                            raceId, result.SkipReason);
                        break;
                }
            }
            catch (Exception ex)
            {
                skipped++;
                items.Add(new DueRaceStartItem(raceId, RaceStartOutcome.Skipped, ex.Message));
                _logger.LogError(ex, "Failed auto-start for race {RaceId}.", raceId);
            }
        }

        return new ProcessDueRaceStartsResponse(
            dueRaceIds.Count,
            started,
            already,
            skipped,
            items);
    }
}
