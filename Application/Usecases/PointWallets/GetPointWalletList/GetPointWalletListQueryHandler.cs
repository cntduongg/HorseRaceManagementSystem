using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PointWallets.GetPointWalletList;

public sealed class GetPointWalletListQueryHandler
    : IRequestHandler<GetPointWalletListQuery, List<PointWalletListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPointWalletListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PointWalletListItemResponse>> Handle(
        GetPointWalletListQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PointWallets.AsNoTracking();

        // Không phải ADMIN → chỉ ví của chính mình.
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
            .Select(x => new PointWalletListItemResponse(
                x.WalletId,
                x.SpectatorId,
                x.Balance,
                x.IsFrozen
            ))
            .ToListAsync(cancellationToken);
    }
}