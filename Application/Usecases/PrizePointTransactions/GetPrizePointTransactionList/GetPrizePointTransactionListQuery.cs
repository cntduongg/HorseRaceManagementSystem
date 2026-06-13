using MediatR;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;

public sealed record GetPrizePointTransactionListQuery()
    : IRequest<List<PrizePointTransactionListItemResponse>>;