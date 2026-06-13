using MediatR;

namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed class GetTournamentListQueryHandler
    : IRequestHandler<GetTournamentListQuery, List<TournamentListItemResponse>>
{
    public Task<List<TournamentListItemResponse>> Handle(
        GetTournamentListQuery request,
        CancellationToken cancellationToken)
    {
        var result = new List<TournamentListItemResponse>
        {
            new(
                TournamentId: 1,
                Name: "Spring Championship",
                StartDate: DateOnly.FromDateTime(DateTime.Today),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                Status: "Open"
            ),
            new(
                TournamentId: 2,
                Name: "Summer Championship",
                StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
                Status: "Draft"
            )
        };

        return Task.FromResult(result);
    }
}