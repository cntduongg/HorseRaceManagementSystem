namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed record RaceResultListItemResponse(
    int RaceId,
    int EntryId,
    decimal TotalPoints,
    int? FinalPosition,
    bool IsRaceDQ,
    int LegWinCount,
    int LegTop3Count,
    string? ViolationNote
);