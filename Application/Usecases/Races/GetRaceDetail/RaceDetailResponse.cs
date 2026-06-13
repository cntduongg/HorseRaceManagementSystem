namespace Application.Usecases.Races.GetRaceDetail;

public sealed record RaceDetailResponse(
    int RaceId,
    int TournamentId,
    string Name,
    DateTime ScheduledStartTime,
    int NumberOfLegs,
    int MaxHorses,
    string RoundType,
    string Status,
    int? Referee1Id,
    int? Referee2Id
);