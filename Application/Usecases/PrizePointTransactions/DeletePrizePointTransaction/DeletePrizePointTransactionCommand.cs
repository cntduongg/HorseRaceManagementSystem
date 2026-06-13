using MediatR;

namespace Application.Usecases.PrizePointTransactions.DeletePrizePointTransaction;

public sealed record DeletePrizePointTransactionCommand(
    int PrizePointTransactionId
) : IRequest<bool>;