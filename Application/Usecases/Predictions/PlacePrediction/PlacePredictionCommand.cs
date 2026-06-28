using MediatR;

namespace Application.Usecases.Predictions.PlacePrediction;

public sealed record PlacePredictionCommand(
    int RaceId,
    int EntryId,
    int SpectatorId,
    decimal BetAmount) : IRequest<PlacePredictionResponse>;