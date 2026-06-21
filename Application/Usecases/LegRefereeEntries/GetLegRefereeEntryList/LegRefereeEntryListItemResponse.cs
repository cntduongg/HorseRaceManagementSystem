namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryList;

public sealed record LegRefereeEntryListItemResponse(
    long LegRefereeEntryId,
    int RaceId,
    int LegNumber,
    int EntryId,
    string ResultStatus
);