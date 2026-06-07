namespace Application.Usecases.Races.GetRaceDetail;

public sealed record RaceDetailResponse(
    Guid RaceId,
    string Name,
    DateTime ScheduledAt,
    int NumberOfLegs,
    int MaxHorses,
    string Status
);