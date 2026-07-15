using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// GET /api/admin/points/{userId} — Xem ví + 20 giao dịch gần nhất của 1 khán giả (tiện kiểm tra khi test).
public sealed record GetUserWalletQuery(int UserId) : IRequest<UserWalletResponse?>;

public sealed record UserWalletTransactionItem(
    int TransactionId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason,
    DateTime CreatedAt);

public sealed record UserWalletResponse(
    int UserId,
    string UserName,
    string UserEmail,
    decimal Balance,
    bool IsFrozen,
    IReadOnlyList<UserWalletTransactionItem> RecentTransactions);

public sealed class GetUserWalletQueryHandler
    : IRequestHandler<GetUserWalletQuery, UserWalletResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetUserWalletQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserWalletResponse?> Handle(
        GetUserWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await (
            from w in _context.PointWallets.AsNoTracking()
            join u in _context.Users.AsNoTracking() on w.SpectatorId equals u.UserId
            where w.SpectatorId == request.UserId
            select new { w.WalletId, w.SpectatorId, u.FullName, u.Email, w.Balance, w.IsFrozen })
            .FirstOrDefaultAsync(cancellationToken);

        if (wallet is null)
            return null;

        var transactions = await _context.WalletTransactions.AsNoTracking()
            .Where(t => t.WalletId == wallet.WalletId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new UserWalletTransactionItem(
                t.WalletTransactionId, t.Type, t.Amount, t.BalanceAfter, t.Reason, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new UserWalletResponse(
            wallet.SpectatorId, wallet.FullName, wallet.Email,
            wallet.Balance, wallet.IsFrozen, transactions);
    }
}
