using MediatR;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record GetRacePredictionOddsQuery(
    int RaceId
) : IRequest<RacePredictionOddsResponse>;