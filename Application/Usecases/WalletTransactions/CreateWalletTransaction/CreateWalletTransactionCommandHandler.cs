using MediatR;

namespace Application.Usecases.WalletTransactions.CreateWalletTransaction;

public sealed class CreateWalletTransactionCommandHandler
    : IRequestHandler<CreateWalletTransactionCommand, int>
{
    public Task<int> Handle(
        CreateWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Validate related entities

        // TODO: Save to database

        var transactionId = 1;

        return Task.FromResult(transactionId);
    }
}