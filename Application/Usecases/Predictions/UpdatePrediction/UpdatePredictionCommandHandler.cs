using MediatR;

namespace Application.Usecases.Predictions.UpdatePrediction;

public sealed class UpdatePredictionCommandHandler
    : IRequestHandler<UpdatePredictionCommand, bool>
{
    public Task<bool> Handle(
        UpdatePredictionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update prediction in database

        return Task.FromResult(true);
    }
}