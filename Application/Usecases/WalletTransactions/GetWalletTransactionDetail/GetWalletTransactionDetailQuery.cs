using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionDetail;

// ViewerSpectatorId: null → ADMIN; có giá trị → chỉ đọc được giao dịch của chính mình.
public sealed record GetWalletTransactionDetailQuery(
    int WalletTransactionId,
    int? ViewerSpectatorId
) : IRequest<WalletTransactionDetailResponse?>;