using MediatR;

namespace Application.Usecases.Races.GetRaceList;

public sealed class GetRaceListQueryHandler
    : IRequestHandler<GetRaceListQuery, List<RaceListItemResponse>>
{
    public Task<List<RaceListItemResponse>> Handle(
        GetRaceListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var races = new List<RaceListItemResponse>
        {
            new(
                1,
                "Race 1",
                DateTime.UtcNow.AddDays(1),
                "Scheduled"
            ),
            new(
                2,
                "Race 2",
                DateTime.UtcNow.AddDays(2),
                "Scheduled"
            )
        };

        return Task.FromResult(races);
    }
}