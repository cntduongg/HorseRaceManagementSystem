namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed record RaceResultListItemResponse(
    int RaceId,
    int EntryId,
    int TotalPoints,
    int? FinalPosition,
    bool IsRaceDQ,
    int LegWinCount,
    int LegTop3Count,
    string? ViolationNote
);