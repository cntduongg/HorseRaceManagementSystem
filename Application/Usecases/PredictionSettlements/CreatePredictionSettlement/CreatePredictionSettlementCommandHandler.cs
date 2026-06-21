using MediatR;

namespace Application.Usecases.PredictionSettlements.CreatePredictionSettlement;

public sealed class CreatePredictionSettlementCommandHandler
	: IRequestHandler<CreatePredictionSettlementCommand, int>
{
	public Task<int> Handle(
		CreatePredictionSettlementCommand request,
		CancellationToken cancellationToken)
	{
		// TODO: Save prediction settlement to database

		var predictionSettlementId = 1;

		return Task.FromResult(predictionSettlementId);
	}
}