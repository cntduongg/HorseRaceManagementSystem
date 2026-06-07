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
            Name: "Test Race",
            ScheduledAt: DateTime.UtcNow.AddDays(7),
            NumberOfLegs: 3,
            MaxHorses: 8,
            Status: "Scheduled"
        );

        return Task.FromResult<RaceDetailResponse?>(response);
    }
}