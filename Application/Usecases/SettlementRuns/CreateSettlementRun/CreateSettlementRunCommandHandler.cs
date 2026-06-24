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

        if (request.TotalPredictions < 0)
            throw new InvalidOperationException("TotalPredictions cannot be negative.");

        if (request.TotalBetAmount < 0)
            throw new InvalidOperationException("TotalBetAmount cannot be negative.");

        if (request.TotalPayoutAmount < 0)
            throw new InvalidOperationException("TotalPayoutAmount cannot be negative.");

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