namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record RacePredictionOddsResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime ScheduledStartTime,
    DateTime? OddsComputedAt,
    List<RacePredictionOddsEntryResponse> Entries);

public sealed record RacePredictionOddsEntryResponse(
    int EntryId,
    int HorseId,
    string? HorseName,
    string? HorseImageUrl,
    int JockeyId,
    string? JockeyName,
    string? JockeyAvatarUrl,
    int HorseOwnerId,
    string? HorseOwnerName,
    int? GateNumber,
    decimal BaseOdds,
    decimal CurrentOdds,
    decimal EntryPool,
    decimal TotalPool);