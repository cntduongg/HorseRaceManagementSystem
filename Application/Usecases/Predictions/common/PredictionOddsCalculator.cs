using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.Common;

public sealed record DynamicEntryOdds(
    int EntryId,
    decimal BaseOdds,
    decimal CurrentOdds,
    decimal EntryPool,
    decimal TotalPool);

public static class PredictionOddsCalculator
{
    private const decimal MinOdds = 1.10m;
    private const decimal MaxOdds = 25.00m;
    private const string EntryApproved = "Approved";

    public static async Task<List<DynamicEntryOdds>> CalculateRaceOddsAsync(
        IApplicationDbContext context,
        int raceId,
        CancellationToken cancellationToken,
        int? placingEntryId = null,
        decimal placingAmount = 0m)
    {
        var approvedEntries = await context.Entries
            .AsNoTracking()
            .Where(e =>
                e.RaceId == raceId &&
                e.Status == EntryApproved &&
                e.Odds > 0)
            .Select(e => new
            {
                e.EntryId,
                BaseOdds = e.Odds
            })
            .ToListAsync(cancellationToken);

        if (approvedEntries.Count == 0)
        {
            return new List<DynamicEntryOdds>();
        }

        var pools = await context.Predictions
            .AsNoTracking()
            .Where(p =>
                p.RaceId == raceId &&
                p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.FirstEntryId)
            .Select(g => new
            {
                EntryId = g.Key,
                Amount = g.Sum(x => x.BetAmount)
            })
            .ToDictionaryAsync(x => x.EntryId, x => x.Amount, cancellationToken);

        if (placingEntryId.HasValue && placingAmount > 0)
        {
            pools[placingEntryId.Value] =
                pools.TryGetValue(placingEntryId.Value, out var current)
                    ? current + placingAmount
                    : placingAmount;
        }

        var totalPool = pools.Values.Sum();

        return approvedEntries
            .Select(entry =>
            {
                pools.TryGetValue(entry.EntryId, out var entryPool);

                var currentOdds = CalculateDynamicOdds(
                    baseOdds: entry.BaseOdds,
                    entryPool: entryPool,
                    totalPool: totalPool,
                    entryCount: approvedEntries.Count);

                return new DynamicEntryOdds(
                    EntryId: entry.EntryId,
                    BaseOdds: entry.BaseOdds,
                    CurrentOdds: currentOdds,
                    EntryPool: entryPool,
                    TotalPool: totalPool);
            })
            .ToList();
    }

    public static async Task<decimal> CalculateEntryOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int entryId,
        decimal placingAmount,
        CancellationToken cancellationToken)
    {
        var odds = await CalculateRaceOddsAsync(
            context,
            raceId,
            cancellationToken,
            placingEntryId: entryId,
            placingAmount: placingAmount);

        var result = odds.FirstOrDefault(x => x.EntryId == entryId);

        if (result is null)
        {
            throw new InvalidOperationException("The entry does not have valid odds to place a prediction.");
        }

        return result.CurrentOdds;
    }

    private static decimal CalculateDynamicOdds(
        decimal baseOdds,
        decimal entryPool,
        decimal totalPool,
        int entryCount)
    {
        if (totalPool <= 0 || entryCount <= 0)
        {
            return ClampAndRound(baseOdds);
        }

        var averagePool = totalPool / entryCount;

        // Nếu chưa ai đặt vào entry này thì odds tăng nhẹ.
        // Nếu nhiều người đặt hơn trung bình thì odds giảm.
        var pressure = entryPool <= 0
            ? 0.5m
            : entryPool / Math.Max(averagePool, 1m);

        var adjustedOdds = baseOdds / (decimal)Math.Sqrt((double)pressure);

        return ClampAndRound(adjustedOdds);
    }

    private static decimal ClampAndRound(decimal odds)
    {
        var clamped = Math.Clamp(odds, MinOdds, MaxOdds);
        return Math.Round(clamped, 2, MidpointRounding.AwayFromZero);
    }
}