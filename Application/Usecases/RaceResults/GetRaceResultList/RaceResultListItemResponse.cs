namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed record RaceResultListItemResponse(
    int RaceId,
    int EntryId,
    string? HorseName,
    string? OwnerName,
    string? JockeyName,
    int? FinalPosition,
    int TotalPoints
);