namespace Application.Usecases.PointWallets.GetPointWalletDetail;

public sealed record PointWalletDetailResponse(
    int WalletId,
    int SpectatorId,
    decimal Balance,
    bool IsFrozen
);