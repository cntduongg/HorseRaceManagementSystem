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
    // Mốc đóng đăng ký = mốc odds được tính. Đây là cờ DUY NHẤT của cửa cược (Flow 7):
    // có mốc này + race còn Scheduled ⇒ spectator cược được, và Start Race đủ điều kiện.
    DateTime? OddsComputedAt,
    DateTime? PublishedAt
);