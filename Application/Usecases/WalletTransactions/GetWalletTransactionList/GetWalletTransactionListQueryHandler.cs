using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

public sealed class GetWalletTransactionListQueryHandler
    : IRequestHandler<
        GetWalletTransactionListQuery,
        List<WalletTransactionListItemResponse>>
{
    public Task<List<WalletTransactionListItemResponse>> Handle(
        GetWalletTransactionListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var list = new List<WalletTransactionListItemResponse>
        {
            new(
                1,
                1,
                "Deposit",
                100,
                500,
                DateTime.UtcNow
            ),
            new(
                2,
                1,
                "Bet",
                -50,
                450,
                DateTime.UtcNow
            )
        };

        return Task.FromResult(list);
    }
}