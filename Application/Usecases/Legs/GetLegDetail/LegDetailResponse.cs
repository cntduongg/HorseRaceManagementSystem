namespace Application.Usecases.Legs.GetLegDetail;

public sealed record LegDetailResponse(
    int RaceId,
    int LegNumber,
    string Status,
    string? ConfirmationType,
    DateTime? ConfirmedAt,
    DateTime? ConflictReportedAt,
    string? AdminOverrideReason,
    DateTime? StartedAt,
    DateTime? FinishedAt
);