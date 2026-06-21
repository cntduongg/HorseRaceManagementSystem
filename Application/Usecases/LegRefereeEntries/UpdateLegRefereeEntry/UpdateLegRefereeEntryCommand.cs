using MediatR;

namespace Application.Usecases.LegRefereeEntries.UpdateLegRefereeEntry;

public sealed record UpdateLegRefereeEntryCommand(
    long LegRefereeEntryId,
    int RaceId,
    int LegNumber,
    int EntryId,
    int RefereeUserId,
    int? FinishPosition,
    string ResultStatus
) : IRequest<bool>;
