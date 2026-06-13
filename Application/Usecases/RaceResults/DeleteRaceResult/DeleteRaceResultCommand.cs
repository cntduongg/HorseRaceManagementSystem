using MediatR;

namespace Application.Usecases.RaceResults.DeleteRaceResult;

public sealed record DeleteRaceResultCommand(
    int RaceId,
    int EntryId
) : IRequest<bool>;