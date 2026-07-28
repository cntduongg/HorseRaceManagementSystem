namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

// Reason: mô tả người dùng đọc được, luôn được ghi lúc tạo transaction. Nội dung do
// Application.Common.Wallet.WalletTransactionReasonBuilder dựng, dạng
// "Bet placed | Race: Kentucky Derby race A | Horse: Groudon | Jockey: Tran Cong Nghia".
// ⚠️ Giao dịch tạo TRƯỚC 2026-07-28 còn giữ format cũ theo id ("Bet on Race #12, Entry #34") —
// Reason lưu thành chuỗi tĩnh trong DB nên bản ghi cũ không tự đổi theo code.
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