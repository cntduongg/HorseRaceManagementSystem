namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

// Reason: mô tả người dùng đọc được, luôn được ghi lúc tạo transaction
// ("Bet on Race #12, Entry #34", "Won bet on race #12, entry #34, odds 4.2"…).
// Thiếu field này thì FE không có gì để hiển thị ngoài `Type` — xem PointWalletPage.fmtTxDesc().
// Endpoint detail đã trả sẵn từ trước, chỉ list là sót.
public sealed record WalletTransactionListItemResponse(
    int WalletTransactionId,
    int WalletId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason,
    DateTime CreatedAt
);