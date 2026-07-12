using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// PUT /api/admin/points/{userId} — Admin ĐẶT số dư ví về một giá trị chính xác (buff điểm phục vụ test).
// Khác với AdjustPoints (cộng/trừ theo delta), đây đặt thẳng balance = giá trị mong muốn.
public sealed record SetPointBalanceCommand(
    int UserId,
    decimal Balance,
    string? Reason,
    int AdminId) : IRequest<SetPointBalanceResponse>;

public sealed record SetPointBalanceResponse(
    int UserId, decimal OldBalance, decimal NewBalance, decimal Delta);

public sealed class SetPointBalanceCommandHandler
    : IRequestHandler<SetPointBalanceCommand, SetPointBalanceResponse>
{
    private readonly IApplicationDbContext _context;

    public SetPointBalanceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SetPointBalanceResponse> Handle(
        SetPointBalanceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Balance < 0)
            throw new InvalidOperationException("Số dư không được âm.");

        var spectatorExists = await _context.Spectators
            .AnyAsync(s => s.UserId == request.UserId, cancellationToken);
        if (!spectatorExists)
            throw new InvalidOperationException("Người dùng không phải khán giả hoặc không tồn tại.");

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

            var oldBalance = wallet.Balance;
            var delta = request.Balance - oldBalance;

            wallet.Balance = request.Balance;
            wallet.UpdatedAt = now;

            // Ghi giao dịch để lịch sử ví khớp số dư mới (delta có thể âm/dương/0).
            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                SpectatorId = request.UserId,
                AdminId = request.AdminId,
                Type = "AdminSet",
                Amount = delta,
                BalanceAfter = wallet.Balance,
                Reason = string.IsNullOrWhiteSpace(request.Reason)
                    ? $"Admin đặt số dư = {request.Balance}"
                    : request.Reason.Trim(),
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new SetPointBalanceResponse(request.UserId, oldBalance, wallet.Balance, delta);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
