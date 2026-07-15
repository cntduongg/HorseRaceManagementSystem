using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// POST /api/admin/points/adjust — Admin cộng/trừ điểm ví thủ công.
public sealed record AdjustPointsCommand(
    int UserId,
    decimal Amount,
    string Type,
    string? Reason,
    int AdminId) : IRequest<AdjustPointsResponse>;

public sealed record AdjustPointsResponse(int UserId, decimal NewBalance);

public sealed class AdjustPointsCommandHandler
    : IRequestHandler<AdjustPointsCommand, AdjustPointsResponse>
{
    private static readonly string[] CreditTypes = { "Credit", "Bonus", "Refund" };

    private readonly IApplicationDbContext _context;

    public AdjustPointsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdjustPointsResponse> Handle(
        AdjustPointsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("The amount must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Adjustment type is required.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A reason is required.");

        var isCredit = CreditTypes.Contains(request.Type, StringComparer.OrdinalIgnoreCase);
        var delta = isCredit ? request.Amount : -request.Amount;

        var spectatorExists = await _context.Spectators
            .AnyAsync(s => s.UserId == request.UserId, cancellationToken);
        if (!spectatorExists)
            throw new InvalidOperationException("The user is not a spectator or does not exist.");

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var wallet = await _context.PointWallets
                .FirstOrDefaultAsync(w => w.SpectatorId == request.UserId, cancellationToken);

            if (wallet is null)
            {
                wallet = new PointWallet
                {
                    SpectatorId = request.UserId,
                    Balance = 0,
                    CreatedAt = now
                };
                _context.PointWallets.Add(wallet);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (wallet.Balance + delta < 0)
                throw new InvalidOperationException("Insufficient balance to deduct.");

            wallet.Balance += delta;
            wallet.UpdatedAt = now;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = request.UserId,
                AdminId = request.AdminId,
                Type = request.Type,
                Amount = delta,
                BalanceAfter = wallet.Balance,
                Reason = request.Reason!.Trim(),
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new AdjustPointsResponse(request.UserId, wallet.Balance);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
