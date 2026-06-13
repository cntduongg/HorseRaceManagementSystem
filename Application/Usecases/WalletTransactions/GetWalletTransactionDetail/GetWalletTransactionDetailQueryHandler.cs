using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionDetail;

public sealed class GetWalletTransactionDetailQueryHandler
    : IRequestHandler<
        GetWalletTransactionDetailQuery,
        WalletTransactionDetailResponse?>
{
    public Task<WalletTransactionDetailResponse?> Handle(
        GetWalletTransactionDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new WalletTransactionDetailResponse(
            request.WalletTransactionId,
            1,
            1,
            "Deposit",
            100,
            500,
            "Initial balance",
            DateTime.UtcNow
        );

        return Task.FromResult<WalletTransactionDetailResponse?>(response);
    }
}