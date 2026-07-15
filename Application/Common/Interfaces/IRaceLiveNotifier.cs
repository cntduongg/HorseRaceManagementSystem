using Application.Usecases.RaceExecution;

namespace Application.Common.Interfaces;

/// <summary>
/// Đẩy snapshot live tới các client đang theo dõi một Race.
/// Hiện thực ở tầng Api bằng SignalR (SignalRRaceLiveNotifier) — Application không phụ thuộc ASP.NET.
/// </summary>
public interface IRaceLiveNotifier
{
    Task PushAsync(
        int raceId,
        RaceLiveResponse snapshot,
        CancellationToken cancellationToken = default);
}
