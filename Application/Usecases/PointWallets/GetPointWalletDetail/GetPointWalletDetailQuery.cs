using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletDetail;

// ViewerSpectatorId: null → ADMIN (đọc ví bất kỳ); có giá trị → chỉ đọc được ví của chính mình.
// Không có filter này thì chỉ cần đoán walletId là xem được số dư người khác.
public sealed record GetPointWalletDetailQuery(
    int WalletId,
    int? ViewerSpectatorId
) : IRequest<PointWalletDetailResponse?>;