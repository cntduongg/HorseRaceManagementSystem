using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PointWallets.GetPointWalletDetail;

public sealed class GetPointWalletDetailQueryHandler
    : IRequestHandler<GetPointWalletDetailQuery, PointWalletDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetPointWalletDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PointWalletDetailResponse?> Handle(
        GetPointWalletDetailQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PointWallets
            .AsNoTracking()
            .Where(x => x.WalletId == request.WalletId);

        // Không phải ADMIN → chỉ ví của chính mình. Trả null (controller → 404) thay vì 403
        // để không lộ việc walletId đó có tồn tại hay không.
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
            .Select(x => new PointWalletDetailResponse(
                x.WalletId,
                x.SpectatorId,
                x.Balance,
                x.IsFrozen
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}