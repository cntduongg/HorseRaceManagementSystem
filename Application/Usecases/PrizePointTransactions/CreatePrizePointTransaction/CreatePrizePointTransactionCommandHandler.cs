using MediatR;

namespace Application.Usecases.PrizePointTransactions.CreatePrizePointTransaction;

public sealed class CreatePrizePointTransactionCommandHandler
    : IRequestHandler<CreatePrizePointTransactionCommand, int>
{
    public Task<int> Handle(
        CreatePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save prize point transaction into database

        var id = 1;

        return Task.FromResult(id);
    }
}