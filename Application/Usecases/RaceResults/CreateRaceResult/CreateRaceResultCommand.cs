using MediatR;

namespace Application.Usecases.RaceResults.CreateRaceResult;

public sealed record CreateRaceResultCommand(
    int RaceId,
    int EntryId,
    int TotalPoints,
    int? FinalPosition,
    bool IsRaceDQ,
    int LegWinCount,
    int LegTop3Count
) : IRequest<bool>;