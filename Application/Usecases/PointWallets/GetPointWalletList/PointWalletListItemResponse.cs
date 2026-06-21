namespace Application.Usecases.PointWallets.GetPointWalletList;

public sealed record PointWalletListItemResponse(
    int WalletId,
    int SpectatorId,
    decimal Balance,
    bool IsFrozen
);