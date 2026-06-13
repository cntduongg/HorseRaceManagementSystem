using MediatR;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

public sealed record GetWalletTransactionListQuery()
    : IRequest<List<WalletTransactionListItemResponse>>;