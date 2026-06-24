namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed record RaceResultDetailResponse(
    int RaceId,
    int EntryId,
    string? HorseName,
    string? OwnerName,
    string? JockeyName,
    int TotalPoints,
    int? FinalPosition,
    bool IsRaceDQ,
    int LegWinCount,
    int LegTop3Count,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);