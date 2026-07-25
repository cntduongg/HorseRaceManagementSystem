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
        var query = _context.WalletTransactions
            .AsNoTracking()
            .Where(x => x.WalletTransactionId == request.WalletTransactionId);

        // Không phải ADMIN → chỉ giao dịch của chính mình (trả null → 404, không lộ tồn tại).
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
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