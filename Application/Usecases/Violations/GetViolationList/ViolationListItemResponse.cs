namespace Application.Usecases.Violations.GetViolationList;

public sealed record ViolationListItemResponse(
    int ViolationId,
    int RaceId,
    string? RaceName,
    int LegNumber,
    int EntryId,
    string? JockeyName,
    string? HorseName,
    string ViolationType,
    string? Description,
    string Penalty,
    string Status,
    int ReportedByRefereeId,
    int? ReviewedByAdminId,
    DateTime? ReviewedAt,
    string? AdminNote,
    DateTime CreatedAt
);
