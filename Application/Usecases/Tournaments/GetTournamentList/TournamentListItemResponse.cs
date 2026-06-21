namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed record TournamentListItemResponse(
	int TournamentId,
	string Name,
	DateOnly StartDate,
	DateOnly EndDate,
	string Status
);