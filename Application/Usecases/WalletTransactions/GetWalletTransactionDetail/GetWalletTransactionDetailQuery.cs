using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionDetail;

public sealed record GetWalletTransactionDetailQuery(
    int WalletTransactionId
) : IRequest<WalletTransactionDetailResponse?>;