namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultList;

public sealed record LegOfficialResultListItemResponse(
    int RaceId,
    int LegNumber,
    int EntryId,
    int? FinishPosition,
    string ResultStatus
);