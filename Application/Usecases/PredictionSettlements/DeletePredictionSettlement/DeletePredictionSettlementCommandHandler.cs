using MediatR;

namespace Application.Usecases.PredictionSettlements.DeletePredictionSettlement;

public sealed class DeletePredictionSettlementCommandHandler
    : IRequestHandler<DeletePredictionSettlementCommand, bool>
{
    public Task<bool> Handle(
        DeletePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete prediction settlement

        return Task.FromResult(true);
    }
}