using MediatR;

namespace Application.Usecases.Entries.CreateEntry;

public sealed record CreateEntryCommand(
    int RaceId,
    int HorseId,
    int JockeyId,
    int HorseOwnerId
) : IRequest<int>;