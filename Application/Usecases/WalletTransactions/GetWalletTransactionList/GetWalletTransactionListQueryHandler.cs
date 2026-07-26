using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.WalletTransactions.GetWalletTransactionList;

public sealed class GetWalletTransactionListQueryHandler
    : IRequestHandler<GetWalletTransactionListQuery, List<WalletTransactionListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetWalletTransactionListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WalletTransactionListItemResponse>> Handle(
        GetWalletTransactionListQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WalletTransactions.AsNoTracking();

        // Không phải ADMIN → chỉ giao dịch của chính mình.
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new WalletTransactionListItemResponse(
                x.WalletTransactionId,
                x.WalletId,
                x.Type,
                x.Amount,
                x.BalanceAfter,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}