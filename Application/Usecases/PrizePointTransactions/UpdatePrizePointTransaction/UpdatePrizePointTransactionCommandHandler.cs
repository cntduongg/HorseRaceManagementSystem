using MediatR;

namespace Application.Usecases.PrizePointTransactions.UpdatePrizePointTransaction;

public sealed class UpdatePrizePointTransactionCommandHandler
    : IRequestHandler<UpdatePrizePointTransactionCommand, bool>
{
    public Task<bool> Handle(
        UpdatePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}