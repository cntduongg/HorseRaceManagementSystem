namespace Application.Usecases.Entries.GetEntryDetail;

public sealed record EntryDetailResponse(
    int EntryId,
    int RaceId,
    int HorseId,
    int JockeyId,
    int HorseOwnerId,
    string Status,
    int? GateNumber
);