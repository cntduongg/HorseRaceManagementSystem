using MediatR;

namespace Application.Usecases.PointWallets.UpdatePointWallet;

public sealed record UpdatePointWalletCommand(
    int WalletId,
    int SpectatorId,
    decimal Balance,
    bool IsFrozen
) : IRequest<bool>;