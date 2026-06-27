using MediatR;

namespace Application.Usecases.Races.PublishOdds;

public sealed record PublishRaceOddsCommand(
    int RaceId
) : IRequest<bool>;