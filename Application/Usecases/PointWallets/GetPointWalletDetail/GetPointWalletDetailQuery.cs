using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletDetail;

public sealed record GetPointWalletDetailQuery(
    int WalletId
) : IRequest<PointWalletDetailResponse?>;