namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;

public sealed record PrizePointTransactionListItemResponse(
    int PrizePointTransactionId,
    int UserId,
    string EntityType,
    int Points,
    string Type
);