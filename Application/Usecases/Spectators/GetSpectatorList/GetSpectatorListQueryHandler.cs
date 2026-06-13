using MediatR;

namespace Application.Usecases.Spectators.GetSpectatorList;

public sealed class GetSpectatorListQueryHandler
    : IRequestHandler<GetSpectatorListQuery, List<SpectatorListItemResponse>>
{
    public Task<List<SpectatorListItemResponse>> Handle(
        GetSpectatorListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load spectators from database

        var spectators = new List<SpectatorListItemResponse>
        {
            new(
                1,
                DateTime.UtcNow.AddDays(-10),
                true
            ),
            new(
                2,
                DateTime.UtcNow.AddDays(-5),
                true
            )
        };

        return Task.FromResult(spectators);
    }
}