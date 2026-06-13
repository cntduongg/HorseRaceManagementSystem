using MediatR;

namespace Application.Usecases.PredictionSettlements.GetPredictionSettlementList;

public sealed record GetPredictionSettlementListQuery()
    : IRequest<List<PredictionSettlementListItemResponse>>;