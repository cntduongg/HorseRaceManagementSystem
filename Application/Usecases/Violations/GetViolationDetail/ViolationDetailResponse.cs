namespace Application.Usecases.Violations.GetViolationDetail;

public sealed record ViolationDetailResponse(
    int ViolationId,
    int RaceId,
    int LegNumber,
    int EntryId,
    int ReportedByRefereeId,
    string ViolationType,
    string? Description,
    string Penalty,
    string Status,
    int? ReviewedByAdminId,
    DateTime? ReviewedAt,
    string? AdminNote,
    DateTime CreatedAt
);