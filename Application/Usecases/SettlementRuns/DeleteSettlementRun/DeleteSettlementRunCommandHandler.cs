using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.SettlementRuns.DeleteSettlementRun;

public sealed class DeleteSettlementRunCommandHandler
    : IRequestHandler<DeleteSettlementRunCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteSettlementRunCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.SettlementRuns
            .FirstOrDefaultAsync(x => x.SettlementRunId == request.SettlementRunId, cancellationToken);

        if (entity is null)
            return false;

        _context.SettlementRuns.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}