using Application.Usecases.RaceExecution;
using MediatR;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class RaceAutoStartOptions
{
    public const string SectionName = "RaceAutoStart";

    /// <summary>Khoảng thời gian giữa các lần quét (mặc định 15 giây).</summary>
    public int IntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Worker in-process: mỗi IntervalSeconds gửi ProcessDueRaceStartsCommand.
/// Giống WeeklyTopUpBackgroundService — phụ thuộc API đang chạy.
/// </summary>
public sealed class RaceAutoStartBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RaceAutoStartBackgroundService> _logger;
    private readonly RaceAutoStartOptions _options;

    /// <summary>Giảm spam log cho cùng một Race bị skip liên tục.</summary>
    private readonly Dictionary<int, string> _lastSkipReasons = new();

    public RaceAutoStartBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RaceAutoStartBackgroundService> logger,
        IOptions<RaceAutoStartOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds));
        _logger.LogInformation(
            "Race auto-start worker started (interval={Interval}s).",
            interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ProcessDueRaceStartsCommand(), stoppingToken);

                if (result.Started > 0 || result.Skipped > 0)
                {
                    _logger.LogInformation(
                        "Due race scan: examined={Examined}, started={Started}, already={Already}, skipped={Skipped}.",
                        result.Examined, result.Started, result.AlreadyStarted, result.Skipped);
                }

                foreach (var item in result.Items)
                {
                    if (item.Outcome == RaceStartOutcome.Skipped && item.SkipReason is not null)
                    {
                        if (_lastSkipReasons.TryGetValue(item.RaceId, out var prev) &&
                            prev == item.SkipReason)
                        {
                            continue; // cùng lý do → không log lại
                        }

                        _lastSkipReasons[item.RaceId] = item.SkipReason;
                    }
                    else if (item.Outcome == RaceStartOutcome.Started)
                    {
                        _lastSkipReasons.Remove(item.RaceId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Race auto-start scan failed, will retry later.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
