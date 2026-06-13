namespace Application.Usecases.Violations.GetViolationList;

public sealed record ViolationListItemResponse(
    int ViolationId,
    int RaceId,
    int LegNumber,
    int EntryId,
    string ViolationType,
    string Penalty,
    string Status
);