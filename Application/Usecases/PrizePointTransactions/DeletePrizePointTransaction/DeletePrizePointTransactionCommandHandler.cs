using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PrizePointTransactions.DeletePrizePointTransaction;

public sealed class DeletePrizePointTransactionCommandHandler
    : IRequestHandler<DeletePrizePointTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeletePrizePointTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.PrizePointTransactions
            .FirstOrDefaultAsync(x =>
                x.PrizePointTransactionId == request.PrizePointTransactionId,
                cancellationToken);

        if (entity is null)
            return false;

        _context.PrizePointTransactions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}