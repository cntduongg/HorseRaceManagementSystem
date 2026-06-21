using MediatR;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileList;

public sealed class GetJockeyProfileListQueryHandler
    : IRequestHandler<
        GetJockeyProfileListQuery,
        List<JockeyProfileListItemResponse>>
{
    public Task<List<JockeyProfileListItemResponse>> Handle(
        GetJockeyProfileListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var profiles = new List<JockeyProfileListItemResponse>
        {
            new(
                1,
                "JOCKEY-001",
                100,
                25
            ),
            new(
                2,
                "JOCKEY-002",
                80,
                15
            )
        };

        return Task.FromResult(profiles);
    }
}