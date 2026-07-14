using Application.Common;
using MediatR;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/close-registration — Admin đóng đăng ký.
public sealed record CloseRegistrationCommand(int RaceId) : ICommand<CloseRegistrationResponse>;

public sealed record CloseRegistrationResponse(
    int RaceId,
    int ApprovedEntries,
    int RejectedEntries);

public sealed class CloseRegistrationCommandHandler
    : IRequestHandler<CloseRegistrationCommand, CloseRegistrationResponse>
{
    private readonly IRaceLifecycleCoordinator _lifecycle;

    public CloseRegistrationCommandHandler(IRaceLifecycleCoordinator lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public async Task<CloseRegistrationResponse> Handle(
        CloseRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycle.CloseRegistrationAsync(request.RaceId, cancellationToken);
        return new CloseRegistrationResponse(
            result.RaceId,
            result.ApprovedEntries,
            result.RejectedEntries);
    }

    // Odds = 1 / winRate (clamp 1.1..25). Chưa có lịch sử → theo cỡ field.
    public static decimal OddsFor(int firsts, int total, int fieldSize)
    {
        double winRate = total > 0
            ? (double)firsts / total
            : 1.0 / Math.Max(fieldSize, 2);

        var odds = (decimal)Math.Round(1.0 / Math.Max(winRate, 0.04), 2);
        return Math.Clamp(odds, 1.1m, 25m);
    }
}
