using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.UnlockUser;

// POST /api/admin/users/{id}/unlock — Admin mở khóa tài khoản (gỡ đóng băng ví nếu là Spectator).
public sealed record UnlockUserCommand(int UserId, int AdminId) : ICommand<UnlockUserResponse>;

public sealed record UnlockUserResponse(int UserId, string Status);

public sealed class UnlockUserCommandHandler
    : IRequestHandler<UnlockUserCommand, UnlockUserResponse>
{
    private readonly IApplicationDbContext _context;

    public UnlockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UnlockUserResponse> Handle(
        UnlockUserCommand request,
        CancellationToken cancellationToken)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.Status != "Locked")
                throw new InvalidOperationException("The account is not in a locked state.");

            var now = DateTime.UtcNow;
            user.IsActive = true;
            user.Status = "Active";
            user.LockedUntil = null;
            user.UpdatedAt = now;

            var spectator = await _context.Spectators
                .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);
            if (spectator is not null)
            {
                spectator.IsActive = true;

                var wallet = await _context.PointWallets
                    .FirstOrDefaultAsync(w => w.SpectatorId == request.UserId, cancellationToken);
                if (wallet is not null)
                {
                    wallet.IsFrozen = false;
                    wallet.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new UnlockUserResponse(user.UserId, user.Status);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
