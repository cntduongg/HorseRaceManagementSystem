using MediatR;

namespace Application.Usecases.Races.GetRaceDetail;

public sealed class GetRaceDetailQueryHandler
    : IRequestHandler<GetRaceDetailQuery, RaceDetailResponse?>
{
    public Task<RaceDetailResponse?> Handle(
        GetRaceDetailQuery request,
        CancellationToken cancellationToken)
    {
        var response = new RaceDetailResponse(
            RaceId: request.RaceId,
            TournamentId: 1,
            Name: "Test Race",
            ScheduledStartTime: DateTime.UtcNow.AddDays(7),
            NumberOfLegs: 3,
            MaxHorses: 8,
            RoundType: "Regular",
            Status: "Scheduled",
            Referee1Id: 1,
            Referee2Id: 2
        );

        return Task.FromResult<RaceDetailResponse?>(response);
    }
}