using Application.Common;
using MediatR;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/start — Referee/Admin fallback sau ScheduledStartTime.
// Worker auto-start cũng dùng IRaceLifecycleCoordinator trực tiếp.
public sealed record StartRaceCommand(int RaceId, int CurrentUserId) : ICommand<StartRaceResponse>;

public sealed record StartRaceResponse(int RaceId, string Status, int TotalLegs);

public sealed class StartRaceCommandHandler
    : IRequestHandler<StartRaceCommand, StartRaceResponse>
{
    private readonly IRaceLifecycleCoordinator _lifecycle;

    public StartRaceCommandHandler(IRaceLifecycleCoordinator lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public async Task<StartRaceResponse> Handle(
        StartRaceCommand request,
        CancellationToken cancellationToken)
    {
        // Manual fallback — FE vẫn gọi /start khi Admin/Referee bấm nút (không kiểm tra giờ ở UI).
        // Không enforce schedule để khớp FE; auto-start do worker lo khi tới ScheduledStartTime.
        // requireBettingLocked: bấm tay thì PHẢI khóa cược trước (Flow 7) — người bấm đang đứng
        // trước màn hình, nên bắt bấm đúng thứ tự thay vì âm thầm đóng sổ cược giúp họ.
        var result = await _lifecycle.StartRaceAsync(
            request.RaceId,
            enforceSchedule: false,
            allowAutoClose: true,
            throwOnFailure: true,
            requireBettingLocked: true,
            cancellationToken);

        return new StartRaceResponse(
            result.RaceId,
            result.Status ?? RaceExecutionConstants.RaceInProgress,
            result.TotalLegs ?? 0);
    }
}
