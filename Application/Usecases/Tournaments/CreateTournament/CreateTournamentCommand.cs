using MediatR;

namespace Application.Usecases.Tournaments.CreateTournament;

public sealed record CreateTournamentCommand(
    string Name,
    string? Description,
    string? Location,
    DateOnly StartDate,
    DateOnly EndDate,
    string? LogoUrl
) : IRequest<int>;