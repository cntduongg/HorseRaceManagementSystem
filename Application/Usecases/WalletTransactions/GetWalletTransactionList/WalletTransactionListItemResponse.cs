namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

public sealed record WalletTransactionListItemResponse(
    int WalletTransactionId,
    int WalletId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    DateTime CreatedAt
);