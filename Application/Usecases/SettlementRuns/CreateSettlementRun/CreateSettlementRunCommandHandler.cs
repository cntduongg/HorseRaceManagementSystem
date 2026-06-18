using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.SettlementRuns.CreateSettlementRun;

public sealed class CreateSettlementRunCommandHandler
    : IRequestHandler<CreateSettlementRunCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateSettlementRunCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Type is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        var entity = new SettlementRun
        {
            RaceId = request.RaceId,
            Type = request.Type.Trim(),
            Status = request.Status.Trim(),
            TotalPredictions = request.TotalPredictions,
            TotalBetAmount = request.TotalBetAmount,
            TotalPayoutAmount = request.TotalPayoutAmount,
            TriggeredByAdminId = request.TriggeredByAdminId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SettlementRuns.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.SettlementRunId;
    }
}