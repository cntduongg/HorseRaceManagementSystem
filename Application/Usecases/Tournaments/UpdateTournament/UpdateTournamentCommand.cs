using MediatR;

namespace Application.Usecases.Tournaments.UpdateTournament;

public sealed record UpdateTournamentCommand(
    int TournamentId,
    string Name,
    string? Description,
    string? Location,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? CancelReason
) : IRequest<bool>;