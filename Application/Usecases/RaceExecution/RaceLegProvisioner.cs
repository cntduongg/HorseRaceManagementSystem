using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Tạo bù Leg 1..NumberOfLegs và gán ExecutionStatus theo trạng thái đóng đăng ký.
/// </summary>
public static class RaceLegProvisioner
{
    public static string ExecutionStatusForNewLeg(Race race) =>
        race.OddsComputedAt is null
            ? LegExecutionStatuses.Pending
            : LegExecutionStatuses.PredictionOpen;

    /// <summary>
    /// Thêm Leg row cho mọi legNumber thiếu trong 1..race.NumberOfLegs.
    /// </summary>
    public static async Task EnsureLegsExistAsync(
        IApplicationDbContext context,
        Race race,
        CancellationToken cancellationToken)
    {
        var executionStatus = ExecutionStatusForNewLeg(race);

        for (var legNumber = 1; legNumber <= race.NumberOfLegs; legNumber++)
        {
            var inNavigation = race.Legs?.Any(l => l.LegNumber == legNumber) == true;
            var inStore = await context.Legs.AnyAsync(
                l => l.RaceId == race.RaceId && l.LegNumber == legNumber,
                cancellationToken);

            if (inNavigation || inStore)
                continue;

            context.Legs.Add(new Leg
            {
                RaceId = race.RaceId,
                LegNumber = legNumber,
                Status = RaceExecutionConstants.LegPending,
                ExecutionStatus = executionStatus
            });
        }
    }

    /// <summary>
    /// Đồng bộ số leg khi race còn Scheduled: thêm thiếu, xóa thừa (có guard).
    /// </summary>
    public static async Task SyncLegCountAsync(
        IApplicationDbContext context,
        Race race,
        int newNumberOfLegs,
        CancellationToken cancellationToken)
    {
        if (race.Status != RaceExecutionConstants.RaceScheduled)
            return;

        var previous = race.NumberOfLegs;
        race.NumberOfLegs = newNumberOfLegs;
        try
        {
            await EnsureLegsExistAsync(context, race, cancellationToken);
        }
        finally
        {
            race.NumberOfLegs = previous;
        }

        var legs = await context.Legs
            .Where(l => l.RaceId == race.RaceId)
            .ToListAsync(cancellationToken);

        var toRemove = legs.Where(l => l.LegNumber > newNumberOfLegs).ToList();
        foreach (var leg in toRemove)
        {
            if (!string.Equals(leg.ExecutionStatus, LegExecutionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Cannot remove Leg {leg.LegNumber}: execution status is {leg.ExecutionStatus} (only Pending legs can be removed).");
            }

            var hasPredictions = await context.Predictions.AnyAsync(
                p => p.RaceId == race.RaceId && p.LegNumber == leg.LegNumber,
                cancellationToken);
            if (hasPredictions)
            {
                throw new InvalidOperationException(
                    $"Cannot remove Leg {leg.LegNumber}: predictions exist for this leg.");
            }

            context.Legs.Remove(leg);
        }
    }
}
