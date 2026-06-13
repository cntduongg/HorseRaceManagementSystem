using MediatR;

namespace Application.Usecases.PointWallets.DeletePointWallet;

public sealed record DeletePointWalletCommand(
    int WalletId
) : IRequest<bool>;