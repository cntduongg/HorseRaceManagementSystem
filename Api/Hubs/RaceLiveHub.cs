using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// Kênh push diễn biến đua trực tiếp (Flow 4). Client join group theo Race rồi nhận event
/// "RaceLiveChanged" mang <see cref="Application.Usecases.RaceExecution.RaceLiveResponse"/>
/// mỗi khi một Leg được chốt / Race đổi trạng thái.
///
/// [Authorize] cấp class, KHÔNG khóa theo role: payload không chứa gì mà một user đã đăng nhập
/// không GET được sẵn qua /api/races/{id}/live. Owner/Jockey rồi cũng sẽ cần trang này.
///
/// LƯU Ý triển khai: group nằm trong RAM của tiến trình. Với 1 instance (hiện tại trên Render) là đủ;
/// nếu chạy ≥2 replica thì IHubContext chỉ với tới client trên chính instance đó → cần Redis backplane.
/// </summary>
[Authorize]
public sealed class RaceLiveHub : Hub
{
    /// <summary>
    /// Đặt dưới /api để thừa hưởng proxy sẵn có của Vite (same-origin trong dev → không dính CORS)
    /// và dùng chung công tắc VITE_API_BASE_URL khi deploy.
    /// </summary>
    public const string Path = "/api/hubs/race-live";

    public static string GroupFor(int raceId) => $"race-{raceId}";

    public Task JoinRace(int raceId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(raceId));

    public Task LeaveRace(int raceId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(raceId));
}
