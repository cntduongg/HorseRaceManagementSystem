using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

public sealed record StartLegCommand(int RaceId, int LegNumber, int CurrentUserId) : ICommand<bool>;

public sealed class StartLegCommandHandler : IRequestHandler<StartLegCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IRaceLiveChangeTracker _liveTracker;

    public StartLegCommandHandler(IApplicationDbContext context, IRaceLiveChangeTracker liveTracker)
    {
        _context = context;
        _liveTracker = liveTracker;
    }

    public async Task<bool> Handle(StartLegCommand request, CancellationToken cancellationToken)
    {
        var race = await _context.Races.FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var leg = await _context.Legs.FirstOrDefaultAsync(l => l.RaceId == request.RaceId && l.LegNumber == request.LegNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Leg not found.");

        if (leg.ExecutionStatus == LegExecutionStatuses.InProgress
            || leg.ExecutionStatus == LegExecutionStatuses.Completed
            || leg.ExecutionStatus == LegExecutionStatuses.AwaitingResult)
            throw new InvalidOperationException("This leg has already started or completed.");

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            leg.ExecutionStatus = LegExecutionStatuses.InProgress;
            leg.StartedAt = now;

            // KHÓA CƯỢC: Chuyển toàn bộ cược Pending của Leg này thành Locked
            var pendingPredictions = await _context.Predictions
                .Where(p => p.RaceId == request.RaceId && p.LegNumber == request.LegNumber && p.Status == PredictionStatus.Pending)
                .ToListAsync(cancellationToken);

            foreach (var pred in pendingPredictions)
            {
                pred.Status = PredictionStatus.Locked;
            }

            // Nếu là Leg 1 và Race đang Scheduled -> Đưa Race lên InProgress
            if (request.LegNumber == 1 && race.Status == "Scheduled")
            {
                race.Status = "InProgress";
                race.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _liveTracker.MarkChanged(request.RaceId);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
