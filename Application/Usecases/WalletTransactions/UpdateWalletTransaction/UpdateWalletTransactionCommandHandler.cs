using MediatR;

namespace Application.Usecases.WalletTransactions.UpdateWalletTransaction;

public sealed class UpdateWalletTransactionCommandHandler
    : IRequestHandler<UpdateWalletTransactionCommand, bool>
{
    public Task<bool> Handle(
        UpdateWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}