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
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Type is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        if (request.TotalPredictions < 0)
            throw new InvalidOperationException("TotalPredictions cannot be negative.");

        if (request.TotalBetAmount < 0)
            throw new InvalidOperationException("TotalBetAmount cannot be negative.");

        if (request.TotalPayoutAmount < 0)
            throw new InvalidOperationException("TotalPayoutAmount cannot be negative.");

        var entity = await _context.SettlementRuns
            .FirstOrDefaultAsync(
                x => x.SettlementRunId == request.SettlementRunId,
                cancellationToken);

        if (entity is null)
            return false;

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race not found.");

        if (request.TriggeredByAdminId.HasValue)
        {
            var adminExists = await _context.Users
                .AnyAsync(x => x.UserId == request.TriggeredByAdminId.Value, cancellationToken);

            if (!adminExists)
                throw new InvalidOperationException("TriggeredByAdmin does not exist.");
        }

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