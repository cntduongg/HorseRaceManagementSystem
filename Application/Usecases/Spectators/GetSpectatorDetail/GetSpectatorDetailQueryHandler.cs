using MediatR;

namespace Application.Usecases.Spectators.GetSpectatorDetail;

public sealed class GetSpectatorDetailQueryHandler
    : IRequestHandler<GetSpectatorDetailQuery, SpectatorDetailResponse?>
{
    public Task<SpectatorDetailResponse?> Handle(
        GetSpectatorDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load spectator from database

        var response = new SpectatorDetailResponse(
            request.UserId,
            DateTime.UtcNow,
            true
        );

        return Task.FromResult<SpectatorDetailResponse?>(response);
    }
}