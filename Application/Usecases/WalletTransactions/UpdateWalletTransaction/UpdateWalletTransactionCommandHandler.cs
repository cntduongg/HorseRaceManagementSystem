using MediatR;

namespace Application.Usecases.WalletTransactions.UpdateWalletTransaction;

public sealed class UpdateWalletTransactionCommandHandler
    : IRequestHandler<UpdateWalletTransactionCommand, bool>
{
    public Task<bool> Handle(
        UpdateWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "WalletTransaction is append-only. Updates are not allowed.");
    }
}