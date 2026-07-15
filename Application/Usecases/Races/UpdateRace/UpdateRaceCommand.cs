using MediatR;

namespace Application.Usecases.Races.UpdateRace;

public sealed record UpdateRaceCommand(
    int RaceId,
    int TournamentId,
    string Name,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    int NumberOfLegs,
    int MaxHorses,
    string RoundType,
    int Referee1Id,
    int Referee2Id
) : IRequest<bool>;