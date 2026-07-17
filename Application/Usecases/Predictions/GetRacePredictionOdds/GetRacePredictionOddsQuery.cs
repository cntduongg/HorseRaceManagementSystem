using MediatR;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record GetLegPredictionOddsQuery(int RaceId, int LegNumber) : IRequest<RacePredictionOddsResponse>;