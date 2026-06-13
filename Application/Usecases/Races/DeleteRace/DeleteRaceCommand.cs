using MediatR;

namespace Application.Usecases.Races.DeleteRace;

public sealed record DeleteRaceCommand(int RaceId)
   : IRequest<bool>;