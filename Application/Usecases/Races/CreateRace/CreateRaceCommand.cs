using MediatR;

namespace Application.Usecases.Races.CreateRace;

public sealed record CreateRaceCommand(
    Guid TournamentId,
    string Name,
    DateTime ScheduledAt,
    int NumberOfLegs,
    int MaxHorses,
    string? RoundType,
    Guid Referee1Id,
    Guid Referee2Id
) : IRequest<Guid>;