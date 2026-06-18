using MediatR;

namespace Application.Usecases.RaceResults.UpdateRaceResult;

public sealed record UpdateRaceResultCommand(
	int RaceId,
	int EntryId,
	int TotalPoints,
	int? FinalPosition,
	bool IsRaceDQ,
	int LegWinCount,
	int LegTop3Count
) : IRequest<bool>;