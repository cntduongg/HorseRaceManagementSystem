namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryDetail;

public sealed record LegRefereeEntryDetailResponse(
    long LegRefereeEntryId,
    int RaceId,
    int LegNumber,
    int EntryId,
    int RefereeUserId,
    int? FinishPosition,
    string ResultStatus,
    DateTime SubmittedAt
);