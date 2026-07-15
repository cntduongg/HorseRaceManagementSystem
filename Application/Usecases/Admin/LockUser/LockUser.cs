using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.LockUser;

// POST /api/admin/users/{id}/lock — Admin khóa tài khoản.
// Nếu là Spectator: hoàn 100% các cược Pending rồi ĐÓNG BĂNG ví (Flow 7).
public sealed record LockUserCommand(int UserId, int AdminId, string? Reason)
    : ICommand<LockUserResponse>;

public sealed record LockUserResponse(int UserId, string Status, int RefundedPredictions);

public sealed class LockUserCommandHandler
    : IRequestHandler<LockUserCommand, LockUserResponse>
{
    private readonly IApplicationDbContext _context;

    public LockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LockUserResponse> Handle(
        LockUserCommand request,
        CancellationToken cancellationToken)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.Status == "Locked")
                throw new InvalidOperationException("The account is already locked.");

            var now = DateTime.UtcNow;
            user.IsActive = false;
            user.Status = "Locked";
            user.UpdatedAt = now;

            var refunded = 0;
            var spectator = await _context.Spectators
                .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

            if (spectator is not null)
            {
                spectator.IsActive = false;

                var wallet = await _context.PointWallets
                    .FirstOrDefaultAsync(w => w.SpectatorId == request.UserId, cancellationToken);

                // Hoàn cược Pending trước khi đóng băng.
                var pending = await _context.Predictions
                    .Where(p => p.SpectatorId == request.UserId && p.Status == "Pending")
                    .ToListAsync(cancellationToken);

                foreach (var p in pending)
                {
                    if (wallet is not null)
                    {
                        wallet.Balance += p.BetAmount;
                        wallet.UpdatedAt = now;
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            WalletId = wallet.WalletId,
                            SpectatorId = request.UserId,
                            PredictionId = p.PredictionId,
                            Type = "BetRefund",
                            Amount = p.BetAmount,
                            BalanceAfter = wallet.Balance,
                            Reason = "Bet refund due to account lock",
                            CreatedAt = now
                        });
                    }
                    p.Status = "Cancelled";
                    p.CancelledAt = now;
                    refunded++;
                }

                if (wallet is not null)
                {
                    wallet.IsFrozen = true;
                    wallet.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new LockUserResponse(user.UserId, user.Status, refunded);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
