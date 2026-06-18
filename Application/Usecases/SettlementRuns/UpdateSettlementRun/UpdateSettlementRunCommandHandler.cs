using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.SettlementRuns.UpdateSettlementRun;

public sealed class UpdateSettlementRunCommandHandler
    : IRequestHandler<UpdateSettlementRunCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateSettlementRunCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Type is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        var entity = await _context.SettlementRuns
            .FirstOrDefaultAsync(x => x.SettlementRunId == request.SettlementRunId, cancellationToken);

        if (entity is null)
            return false;

        entity.RaceId = request.RaceId;
        entity.Type = request.Type.Trim();
        entity.Status = request.Status.Trim();
        entity.TotalPredictions = request.TotalPredictions;
        entity.TotalBetAmount = request.TotalBetAmount;
        entity.TotalPayoutAmount = request.TotalPayoutAmount;
        entity.TriggeredByAdminId = request.TriggeredByAdminId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}