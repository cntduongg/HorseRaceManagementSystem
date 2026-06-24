namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;

public sealed record PrizePointTransactionListItemResponse(
    int PrizePointTransactionId,
    int UserId,
    string SourceType,
    int Points,
    string TransactionType
);