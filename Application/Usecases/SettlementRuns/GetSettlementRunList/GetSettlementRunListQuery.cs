using MediatR;

namespace Application.Usecases.SettlementRuns.GetSettlementRunList;

public sealed record GetSettlementRunListQuery()
	: IRequest<List<SettlementRunListItemResponse>>;