using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

// ViewerSpectatorId: null → ADMIN (toàn bộ giao dịch); có giá trị → chỉ giao dịch ví của chính mình.
// Trước đây trả TOÀN BỘ lịch sử giao dịch cho mọi user đã đăng nhập rồi để FE lọc theo `walletId`
// (`PointWalletPage`), tức lộ mọi lần nạp/cược/hoàn của tất cả khán giả.
public sealed record GetWalletTransactionListQuery(int? ViewerSpectatorId)
    : IRequest<List<WalletTransactionListItemResponse>>;