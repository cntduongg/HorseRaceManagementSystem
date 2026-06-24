using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.WalletTransactions.UpdateWalletTransaction;

public sealed class UpdateWalletTransactionCommandHandler
    : IRequestHandler<UpdateWalletTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateWalletTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.WalletTransactions
            .FirstOrDefaultAsync(x => x.WalletTransactionId == request.WalletTransactionId,
                cancellationToken);

        if (entity is null)
            return false;

        // ⚠️ WARNING: financial record mutation (not recommended)
        entity.Type = request.Type.Trim();
        entity.Amount = request.Amount;
        entity.BalanceAfter = request.BalanceAfter;
        entity.Reason = request.Reason;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}