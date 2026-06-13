using MediatR;

namespace Application.Usecases.WalletTransactions.CreateWalletTransaction;

public sealed record CreateWalletTransactionCommand(
    int WalletId,
    int SpectatorId,
    int? PredictionId,
    int? SettlementRunId,
    int? AdminId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason,
    int? RollbackOfTransactionId
) : IRequest<int>;