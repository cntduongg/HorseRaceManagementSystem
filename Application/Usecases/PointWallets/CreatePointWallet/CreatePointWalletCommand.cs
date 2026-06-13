using MediatR;

namespace Application.Usecases.PointWallets.CreatePointWallet;

public sealed record CreatePointWalletCommand(
    int SpectatorId,
    decimal Balance,
    bool IsFrozen
) : IRequest<int>;