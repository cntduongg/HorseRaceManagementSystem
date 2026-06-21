namespace Application.Usecases.Tournaments.GetTournamentDetail;

public sealed record TournamentDetailResponse(
    int TournamentId,
    string Name,
    string? Description,
    string? Location,
    DateOnly StartDate,
    DateOnly EndDate,
    string? LogoUrl,
    string Status,
    string? CancelReason
);