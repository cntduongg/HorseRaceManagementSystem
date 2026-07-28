using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Tạo bù Leg 1..NumberOfLegs.
/// </summary>
public static class RaceLegProvisioner
{
    /// <summary>
    /// Thêm Leg row cho mọi legNumber thiếu trong 1..race.NumberOfLegs.
    /// </summary>
    public static async Task EnsureLegsExistAsync(
        IApplicationDbContext context,
        Race race,
        CancellationToken cancellationToken)
    {
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
                Status = RaceExecutionConstants.LegPending
            });
        }
    }

    /// <summary>
    /// Đồng bộ số leg khi race còn Scheduled: thêm thiếu, xóa thừa.
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
        if (toRemove.Count > 0)
        {
            var hasPredictions = await context.Predictions.AnyAsync(
                p => p.RaceId == race.RaceId,
                cancellationToken);
            if (hasPredictions)
            {
                throw new InvalidOperationException(
                    "Cannot reduce number of legs: predictions exist for this race.");
            }

            foreach (var leg in toRemove)
                context.Legs.Remove(leg);
        }
    }
}
