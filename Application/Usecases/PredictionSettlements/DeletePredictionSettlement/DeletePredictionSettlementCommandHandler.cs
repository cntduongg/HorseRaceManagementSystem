using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PredictionSettlements.DeletePredictionSettlement;

public sealed class DeletePredictionSettlementCommandHandler
    : IRequestHandler<DeletePredictionSettlementCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeletePredictionSettlementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePredictionSettlementCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.PredictionSettlements
            .FirstOrDefaultAsync(x => x.PredictionSettlementId == request.PredictionSettlementId,
                cancellationToken);

        if (entity is null)
            return false;

        _context.PredictionSettlements.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}