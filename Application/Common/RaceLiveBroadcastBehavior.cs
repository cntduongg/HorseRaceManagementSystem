using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common;

/// <summary>
/// Đẩy snapshot live (SignalR) cho các Race được handler đánh dấu qua <see cref="IRaceLiveChangeTracker"/>.
///
/// ⚠️ PHẢI đăng ký NGOÀI <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> (tức đăng ký TRƯỚC nó
/// trong AddApplication) — MediatR lồng behavior theo thứ tự đăng ký, cái đăng ký trước nằm ngoài.
/// Nhờ vậy đoạn sau `await next()` ở đây mới chạy SAU khi UnitOfWork đã SaveChanges.
/// Đăng ký sau UnitOfWork → đẩy trước commit → client refetch trúng dữ liệu cũ.
/// </summary>
public sealed class RaceLiveBroadcastBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRaceLiveChangeTracker _tracker;
    private readonly IRaceLiveNotifier _notifier;
    private readonly ISender _sender;
    private readonly ILogger<RaceLiveBroadcastBehavior<TRequest, TResponse>> _logger;

    public RaceLiveBroadcastBehavior(
        IRaceLiveChangeTracker tracker,
        IRaceLiveNotifier notifier,
        ISender sender,
        ILogger<RaceLiveBroadcastBehavior<TRequest, TResponse>> logger)
    {
        _tracker = tracker;
        _notifier = notifier;
        _sender = sender;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(); // UnitOfWorkBehavior đã SaveChanges bên trong lời gọi này.

        // Drain TRƯỚC khi Send: lời gọi GetRaceLiveQuery bên dưới sẽ đi lại chính behavior này.
        // An toàn CHỈ VÌ set đã được xóa ở đây (và query không đánh dấu gì) → lượt lồng lặp qua
        // danh sách rỗng. Đảo thứ tự drain/send là đệ quy vô hạn.
        var raceIds = _tracker.Drain();

        foreach (var raceId in raceIds)
        {
            try
            {
                var snapshot = await _sender.Send(new GetRaceLiveQuery(raceId), cancellationToken);
                await _notifier.PushAsync(raceId, snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                // Đẩy hỏng KHÔNG được làm hỏng request đã commit thành công.
                // Client vẫn còn poll dự phòng 30s để bắt kịp.
                _logger.LogError(
                    ex,
                    "Failed to push race-live snapshot for race {RaceId} after {RequestName}.",
                    raceId,
                    typeof(TRequest).Name);
            }
        }

        return response;
    }
}
