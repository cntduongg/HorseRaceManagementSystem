using MediatR;

namespace Application.Usecases.PrizePointTransactions.UpdatePrizePointTransaction;

public sealed record UpdatePrizePointTransactionCommand(
    int PrizePointTransactionId,
    string EntityType,
    int FinalPosition,
    int Points,
    string Type
) : IRequest<bool>;