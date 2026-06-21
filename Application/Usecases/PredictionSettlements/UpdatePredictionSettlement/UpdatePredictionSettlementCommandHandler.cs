using MediatR;

namespace Application.Usecases.PredictionSettlements.UpdatePredictionSettlement;

public sealed class UpdatePredictionSettlementCommandHandler
    : IRequestHandler<UpdatePredictionSettlementCommand, bool>
{
    public Task<bool> Handle(
        UpdatePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update prediction settlement

        return Task.FromResult(true);
    }
}