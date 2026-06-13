using MediatR;

namespace Application.Usecases.PrizePointTransactions.DeletePrizePointTransaction;

public sealed class DeletePrizePointTransactionCommandHandler
    : IRequestHandler<DeletePrizePointTransactionCommand, bool>
{
    public Task<bool> Handle(
        DeletePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}