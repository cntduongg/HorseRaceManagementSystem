using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PrizePointTransactions.UpdatePrizePointTransaction;

public sealed class UpdatePrizePointTransactionCommandHandler
    : IRequestHandler<UpdatePrizePointTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdatePrizePointTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdatePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.PrizePointTransactions
            .FirstOrDefaultAsync(x =>
                x.PrizePointTransactionId == request.PrizePointTransactionId,
                cancellationToken);

        if (entity is null)
            return false;

        if (string.IsNullOrWhiteSpace(request.SourceType))
            throw new InvalidOperationException("SourceType is required.");

        if (request.Points < 0)
            throw new InvalidOperationException("Points must be >= 0.");

        entity.SourceType = request.SourceType.Trim();
        entity.FinalPosition = request.FinalPosition;
        entity.Points = request.Points;
        entity.TransactionType = request.TransactionType;
        entity.RollbackOfId = request.RollbackOfId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}