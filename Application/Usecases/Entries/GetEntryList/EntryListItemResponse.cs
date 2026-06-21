namespace Application.Usecases.Entries.GetEntryList;

public sealed record EntryListItemResponse(
    int EntryId,
    int RaceId,
    int HorseId,
    string Status
);