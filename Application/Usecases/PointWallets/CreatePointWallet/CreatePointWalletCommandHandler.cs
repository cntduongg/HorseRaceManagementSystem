using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PointWallets.CreatePointWallet;

public sealed class CreatePointWalletCommandHandler
    : IRequestHandler<CreatePointWalletCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePointWalletCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");

        if (request.Balance < 0)
            throw new InvalidOperationException("Balance cannot be negative.");

        var spectatorExists = await _context.Spectators
            .AnyAsync(x => x.UserId == request.SpectatorId, cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator does not exist.");

        var exists = await _context.PointWallets
            .AnyAsync(x => x.SpectatorId == request.SpectatorId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Wallet already exists for this spectator.");

        var wallet = new PointWallet
        {
            SpectatorId = request.SpectatorId,
            Balance = request.Balance,
            IsFrozen = request.IsFrozen,
            CreatedAt = DateTime.UtcNow
        };

        _context.PointWallets.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);

        return wallet.WalletId;
    }
}