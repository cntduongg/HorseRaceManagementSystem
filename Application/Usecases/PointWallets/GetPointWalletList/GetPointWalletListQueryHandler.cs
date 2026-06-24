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
        return await _context.PointWallets
            .Select(x => new PointWalletListItemResponse(
                x.WalletId,
                x.SpectatorId,
                x.Balance,
                x.IsFrozen
            ))
            .ToListAsync(cancellationToken);
    }
}