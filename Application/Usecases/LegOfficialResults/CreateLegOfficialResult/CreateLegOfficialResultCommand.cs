using MediatR;

namespace Application.Usecases.LegOfficialResults.CreateLegOfficialResult;

public sealed record CreateLegOfficialResultCommand(
    int RaceId,
    int LegNumber,
    int EntryId,
    int? FinishPosition,
    string ResultStatus,
    decimal LegPoints,
    string ConfirmationType,
    int? ConfirmedByAdminId,
    string? OverrideReason
) : IRequest<bool>;