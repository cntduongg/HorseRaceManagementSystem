using MediatR;
using Domain.Aggregates.Enums;

namespace Application.Usecases.PrizePointTransactions.UpdatePrizePointTransaction;

public sealed record UpdatePrizePointTransactionCommand(
    int PrizePointTransactionId,
    string SourceType,
    int FinalPosition,
    int Points,
    PrizePointTransactionType TransactionType,
    int? RollbackOfId
) : IRequest<bool>;