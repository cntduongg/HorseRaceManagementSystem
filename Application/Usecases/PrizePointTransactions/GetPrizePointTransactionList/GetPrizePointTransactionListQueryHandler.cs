using MediatR;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;

public sealed class GetPrizePointTransactionListQueryHandler
    : IRequestHandler<GetPrizePointTransactionListQuery,
        List<PrizePointTransactionListItemResponse>>
{
    public Task<List<PrizePointTransactionListItemResponse>> Handle(
        GetPrizePointTransactionListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var result = new List<PrizePointTransactionListItemResponse>
        {
            new(1, 1, "HorseOwner", 100, "Awarded"),
            new(2, 2, "Jockey", 50, "Awarded")
        };

        return Task.FromResult(result);
    }
}