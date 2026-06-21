using MediatR;

namespace Application.Usecases.WalletTransactions.DeleteWalletTransaction;

public sealed record DeleteWalletTransactionCommand(
    int WalletTransactionId
) : IRequest<bool>;