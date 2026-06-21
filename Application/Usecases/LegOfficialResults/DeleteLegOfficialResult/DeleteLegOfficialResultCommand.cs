using MediatR;

namespace Application.Usecases.LegOfficialResults.DeleteLegOfficialResult;

public sealed record DeleteLegOfficialResultCommand(
    int RaceId,
    int LegNumber,
    int EntryId
) : IRequest<bool>;