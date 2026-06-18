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
        return await _context.PointWallets
            .Where(x => x.WalletId == request.WalletId)
            .Select(x => new PointWalletDetailResponse(
                x.WalletId,
                x.SpectatorId,
                x.Balance,
                x.IsFrozen
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}