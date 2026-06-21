using MediatR;

namespace Application.Usecases.Predictions.DeletePrediction;

public sealed class DeletePredictionCommandHandler
    : IRequestHandler<DeletePredictionCommand, bool>
{
    public Task<bool> Handle(
        DeletePredictionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete prediction from database

        return Task.FromResult(true);
    }
}