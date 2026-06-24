using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PointWallets.UpdatePointWallet;

public sealed class UpdatePointWalletCommandHandler
    : IRequestHandler<UpdatePointWalletCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdatePointWalletCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdatePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WalletId <= 0)
            throw new InvalidOperationException("WalletId is required.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");

        if (request.Balance < 0)
            throw new InvalidOperationException("Balance cannot be negative.");

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(
                x => x.WalletId == request.WalletId,
                cancellationToken);

        if (wallet is null)
            return false;

        var spectatorExists = await _context.Spectators
            .AnyAsync(
                x => x.UserId == request.SpectatorId,
                cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator does not exist.");

        var duplicatedWallet = await _context.PointWallets
            .AnyAsync(x =>
                x.SpectatorId == request.SpectatorId &&
                x.WalletId != request.WalletId,
                cancellationToken);

        if (duplicatedWallet)
            throw new InvalidOperationException(
                "Wallet already exists for this spectator.");

        wallet.SpectatorId = request.SpectatorId;
        wallet.Balance = request.Balance;
        wallet.IsFrozen = request.IsFrozen;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}