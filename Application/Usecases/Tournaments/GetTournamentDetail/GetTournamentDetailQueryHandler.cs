using MediatR;

namespace Application.Usecases.Tournaments.GetTournamentDetail;

public sealed class GetTournamentDetailQueryHandler
    : IRequestHandler<GetTournamentDetailQuery, TournamentDetailResponse?>
{
    public Task<TournamentDetailResponse?> Handle(
        GetTournamentDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
        {
            return Task.FromResult<TournamentDetailResponse?>(null);
        }

        var response = new TournamentDetailResponse(
            TournamentId: request.TournamentId,
            Name: "Demo Tournament",
            Description: "Sample description",
            Location: "Ho Chi Minh City",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            LogoUrl: null,
            Status: "Draft",
            CancelReason: null
        );

        return Task.FromResult<TournamentDetailResponse?>(response);
    }
}