using MediatR;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;

public sealed record GetPrizePointTransactionDetailQuery(
    int PrizePointTransactionId
) : IRequest<PrizePointTransactionDetailResponse?>;