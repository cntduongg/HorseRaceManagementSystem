using Api.Hubs;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

/// <inheritdoc cref="IRaceLiveNotifier"/>
public sealed class SignalRRaceLiveNotifier : IRaceLiveNotifier
{
    /// <summary>Tên event phía client (FE lắng nghe đúng chuỗi này trong useRaceLiveHub).</summary>
    public const string EventName = "RaceLiveChanged";

    private readonly IHubContext<RaceLiveHub> _hub;

    public SignalRRaceLiveNotifier(IHubContext<RaceLiveHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(
        int raceId,
        RaceLiveResponse snapshot,
        CancellationToken cancellationToken = default)
        => _hub.Clients
            .Group(RaceLiveHub.GroupFor(raceId))
            .SendAsync(EventName, snapshot, cancellationToken);
}
