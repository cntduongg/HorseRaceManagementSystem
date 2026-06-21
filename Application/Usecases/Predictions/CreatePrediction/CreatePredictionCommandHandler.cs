using MediatR;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed class CreatePredictionCommandHandler
    : IRequestHandler<CreatePredictionCommand, int>
{
    public Task<int> Handle(
        CreatePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BetAmount < 10)
        {
            throw new InvalidOperationException(
                "BetAmount must be at least 10.");
        }

        if (request.FirstEntryId == request.SecondEntryId ||
            request.FirstEntryId == request.ThirdEntryId ||
            request.SecondEntryId == request.ThirdEntryId)
        {
            throw new InvalidOperationException(
                "Selected entries must be different.");
        }

        // TODO: Save prediction into database

        var predictionId = 1;

        return Task.FromResult(predictionId);
    }
}