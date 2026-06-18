using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.WalletTransactions.DeleteWalletTransaction;

public sealed class DeleteWalletTransactionCommandHandler
    : IRequestHandler<DeleteWalletTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteWalletTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.WalletTransactions
            .FirstOrDefaultAsync(x => x.WalletTransactionId == request.WalletTransactionId,
                cancellationToken);

        if (entity is null)
            return false;

        _context.WalletTransactions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}