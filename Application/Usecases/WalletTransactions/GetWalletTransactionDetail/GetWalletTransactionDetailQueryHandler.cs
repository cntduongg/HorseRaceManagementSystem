using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionDetail;

public sealed class GetWalletTransactionDetailQueryHandler
    : IRequestHandler<GetWalletTransactionDetailQuery, WalletTransactionDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetWalletTransactionDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WalletTransactionDetailResponse?> Handle(
        GetWalletTransactionDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.WalletTransactions
            .AsNoTracking()
            .Where(x => x.WalletTransactionId == request.WalletTransactionId)
            .Select(x => new WalletTransactionDetailResponse(
                x.WalletTransactionId,
                x.WalletId,
                x.SpectatorId,
                x.Type,
                x.Amount,
                x.BalanceAfter,
                x.Reason,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}