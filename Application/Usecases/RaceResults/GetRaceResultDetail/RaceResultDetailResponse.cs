namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed record RaceResultDetailResponse(
    int RaceId,
    int EntryId,
    int TotalPoints,
    int? FinalPosition,
    bool IsRaceDQ,
    int LegWinCount,
    int LegTop3Count,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);