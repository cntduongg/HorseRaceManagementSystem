using MediatR;

namespace Application.Usecases.WalletTransactions.DeleteWalletTransaction;

public sealed class DeleteWalletTransactionCommandHandler
    : IRequestHandler<DeleteWalletTransactionCommand, bool>
{
    public Task<bool> Handle(
        DeleteWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}