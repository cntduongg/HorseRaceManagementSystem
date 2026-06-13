namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultDetail;

public sealed record LegOfficialResultDetailResponse(
    int RaceId,
    int LegNumber,
    int EntryId,
    int? FinishPosition,
    string ResultStatus,
    int LegPoints,
    string ConfirmationType,
    DateTime ConfirmedAt,
    int? ConfirmedByAdminId,
    string? OverrideReason
);