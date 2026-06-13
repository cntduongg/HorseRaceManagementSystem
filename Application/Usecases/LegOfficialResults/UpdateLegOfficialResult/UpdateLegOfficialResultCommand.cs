using MediatR;

namespace Application.Usecases.LegOfficialResults.UpdateLegOfficialResult;

public sealed record UpdateLegOfficialResultCommand(
    int RaceId,
    int LegNumber,
    int EntryId,
    int? FinishPosition,
    string ResultStatus,
    int LegPoints,
    string ConfirmationType,
    int? ConfirmedByAdminId,
    string? OverrideReason
) : IRequest<bool>;