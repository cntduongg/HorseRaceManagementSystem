namespace Application.Usecases.Races.GetRaceList;

public sealed record RaceListItemResponse(
	int RaceId,
	string Name,
	DateTime ScheduledAt,
	string Status
);