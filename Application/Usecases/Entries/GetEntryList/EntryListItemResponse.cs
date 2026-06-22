namespace Application.Usecases.Entries.GetEntryList;

public sealed record EntryListItemResponse(
    int EntryId,
    int RaceId,
    int HorseId,
    string? HorseName,
    string? HorseImageUrl,
    int JockeyId,
    string? JockeyName,
    string? JockeyAvatarUrl,
    int HorseOwnerId,
    string? HorseOwnerName,
    int? GateNumber,
    string Status,
    DateTime SubmittedAt,
    DateTime? ApprovedAt,
    decimal? CurrentOdds
);