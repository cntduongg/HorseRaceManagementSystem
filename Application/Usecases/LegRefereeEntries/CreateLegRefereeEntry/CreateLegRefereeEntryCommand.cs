using MediatR;

namespace Application.Usecases.LegRefereeEntries.CreateLegRefereeEntry;

public sealed record CreateLegRefereeEntryCommand(
    int RaceId,
    int LegNumber,
    int EntryId,
    int RefereeUserId,
    int? FinishPosition,
    string ResultStatus
) : IRequest<long>;