using MediatR;

namespace Application.Usecases.WalletTransactions.UpdateWalletTransaction;

public sealed record UpdateWalletTransactionCommand(
    int WalletTransactionId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason
) : IRequest<bool>;