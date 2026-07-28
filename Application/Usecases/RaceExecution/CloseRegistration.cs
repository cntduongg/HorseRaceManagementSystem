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
}
