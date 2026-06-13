using MediatR;

namespace Application.Usecases.PredictionSettlements.DeletePredictionSettlement;

public sealed record DeletePredictionSettlementCommand(
    int PredictionSettlementId
) : IRequest<bool>;