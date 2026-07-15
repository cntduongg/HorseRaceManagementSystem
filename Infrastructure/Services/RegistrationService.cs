using Application.Common;
using Application.Usecases.RaceExecution;

namespace Infrastructure.Services;

/// <summary>
/// Giữ IRegistrationService tương thích — ủy quyền sang logic canonical của RaceLifecycleCoordinator.
/// </summary>
public sealed class RegistrationService : IRegistrationService
{
    private readonly IRaceLifecycleCoordinator _lifecycle;

    public RegistrationService(IRaceLifecycleCoordinator lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public async Task CloseRegistrationAsync(
        int raceId,
        CancellationToken cancellationToken = default)
    {
        await _lifecycle.CloseRegistrationAsync(raceId, cancellationToken);
    }
}
