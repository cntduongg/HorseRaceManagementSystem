using MediatR;

namespace Application.Usecases.Races.CreateRace;

public sealed record CreateRaceCommand(
    int TournamentId,
    string Name,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    int NumberOfLegs,
    int MaxHorses,
    string RoundType,
    int Referee1Id,
    int Referee2Id
) : IRequest<int>;