using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PointWallets.DeletePointWallet;

public sealed class DeletePointWalletCommandHandler
    : IRequestHandler<DeletePointWalletCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeletePointWalletCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(x => x.WalletId == request.WalletId, cancellationToken);

        if (wallet is null)
            return false;

        _context.PointWallets.Remove(wallet);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}