namespace Application.Usecases.WalletTransactions.GetWalletTransactionDetail;

public sealed record WalletTransactionDetailResponse(
    int WalletTransactionId,
    int WalletId,
    int SpectatorId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason,
    DateTime CreatedAt
);