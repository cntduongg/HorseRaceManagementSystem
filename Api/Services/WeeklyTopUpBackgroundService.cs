using Application.Usecases.Admin.PointsManagement;
using MediatR;

namespace Api.Services;

// Flow 7 — Tác vụ nền: cộng +100 vào ví Spectator mỗi thứ Hai 00:00.
// Chạy idempotent mỗi giờ (bắt kịp nếu app từng tắt qua mốc thứ Hai); logic chống cộng trùng nằm trong handler.
public sealed class WeeklyTopUpBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeeklyTopUpBackgroundService> _logger;

    public WeeklyTopUpBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WeeklyTopUpBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new RunWeeklyTopUpCommand(), stoppingToken);

                if (result.WalletsToppedUp > 0)
                    _logger.LogInformation(
                        "Weekly top-up: +100 cho {Count} ví (tuần {Week:yyyy-MM-dd}).",
                        result.WalletsToppedUp, result.WeekStart);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekly top-up thất bại, sẽ thử lại sau.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
