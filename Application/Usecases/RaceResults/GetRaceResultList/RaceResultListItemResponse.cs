namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed record RaceResultListItemResponse(
    int RaceId,
    int EntryId,
    int? FinalPosition,
    int TotalPoints
);