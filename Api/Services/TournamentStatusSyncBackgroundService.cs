using Application.Usecases.Tournaments.ProcessDueTournamentStatuses;
using MediatR;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class TournamentStatusSyncOptions
{
    public const string SectionName = "TournamentStatusSync";

    /// <summary>Khoảng cách giữa 2 lần quét (mặc định 60 phút).</summary>
    public int IntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Worker in-process: định kỳ gửi <see cref="ProcessDueTournamentStatusesCommand"/> để Tournament
/// tự chuyển Draft/Open → Ongoing → Finished theo ngày.
///
/// Cố ý **thưa hơn nhiều** so với <see cref="RaceAutoStartBackgroundService"/> (15 giây): Race so theo
/// giờ phút nên cần quét dày, Tournament chỉ so theo NGÀY nên mốc chuyển chỉ đổi lúc nửa đêm UTC.
/// Quét ngay một lần lúc khởi động để không phải chờ hết một chu kỳ sau khi deploy.
/// </summary>
public sealed class TournamentStatusSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TournamentStatusSyncBackgroundService> _logger;
    private readonly TournamentStatusSyncOptions _options;

    public TournamentStatusSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TournamentStatusSyncBackgroundService> logger,
        IOptions<TournamentStatusSyncOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        _logger.LogInformation(
            "Tournament status sync worker started (interval={Interval}m).",
            interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var result = await sender.Send(
                    new ProcessDueTournamentStatusesCommand(),
                    stoppingToken);

                if (result.Updated > 0)
                {
                    _logger.LogInformation(
                        "Tournament status scan: examined={Examined}, updated={Updated}.",
                        result.Examined, result.Updated);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tournament status scan failed, will retry later.");
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
