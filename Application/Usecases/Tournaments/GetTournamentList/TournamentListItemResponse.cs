namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed record TournamentListItemResponse(
    int TournamentId,
    string Name,
    string? Location,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int RaceCount
);