namespace Application.Usecases.Races.GetRaceList;

public sealed record RaceListItemResponse(
    int RaceId,
    int TournamentId,
    string? TournamentName,
    string Name,
    DateTime ScheduledAt,
    DateTime ScheduledEndTime,
    int NumberOfLegs,
    int MaxHorses,
    string RoundType,
    string Status,
    int? Referee1Id,
    string? Referee1Name,
    string? Referee1AvatarUrl,
    int? Referee2Id,
    string? Referee2Name,
    string? Referee2AvatarUrl,
    DateTime? RegistrationOpenAt,
    DateTime? RegistrationCloseAt,
    DateTime? OddsComputedAt,
    // Mốc Admin công bố odds cho spectator / khóa sổ cược (Flow 7). FE dùng để bật-tắt
    // nút Publish Odds · Lock Betting · Start Race.
    DateTime? OddsPublishedAt,
    DateTime? BettingLockedAt,
    DateTime? PublishedAt
);