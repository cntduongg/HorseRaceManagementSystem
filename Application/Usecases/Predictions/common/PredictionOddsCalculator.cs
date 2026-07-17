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

    public static async Task<List<DynamicEntryOdds>> CalculateLegOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int legNumber,
        CancellationToken cancellationToken,
        int? placingEntryId = null,
        decimal placingAmount = 0m)
    {
        var approvedEntries = await context.Entries
            .AsNoTracking()
            .Where(e => e.RaceId == raceId && e.Status == EntryApproved && e.Odds > 0)
            .Select(e => new { e.EntryId, BaseOdds = e.Odds })
            .ToListAsync(cancellationToken);

        if (approvedEntries.Count == 0) return new List<DynamicEntryOdds>();

        // Chỉ lấy cược thuộc Leg cụ thể này
        var pools = await context.Predictions
            .AsNoTracking()
            .Where(p => p.RaceId == raceId && p.LegNumber == legNumber && p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.FirstEntryId)
            .Select(g => new { EntryId = g.Key, Amount = g.Sum(x => x.BetAmount) })
            .ToDictionaryAsync(x => x.EntryId, x => x.Amount, cancellationToken);

        if (placingEntryId.HasValue && placingAmount > 0)
        {
            pools[placingEntryId.Value] = pools.TryGetValue(placingEntryId.Value, out var current)
                ? current + placingAmount
                : placingAmount;
        }

        var totalPool = pools.Values.Sum();

        return approvedEntries
            .Select(entry =>
            {
                pools.TryGetValue(entry.EntryId, out var entryPool);
                var currentOdds = CalculateDynamicOdds(entry.BaseOdds, entryPool, totalPool, approvedEntries.Count);
                return new DynamicEntryOdds(entry.EntryId, entry.BaseOdds, currentOdds, entryPool, totalPool);
            })
            .ToList();
    }

    public static async Task<decimal> CalculateEntryLegOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int legNumber,
        int entryId,
        decimal placingAmount,
        CancellationToken cancellationToken)
    {
        var odds = await CalculateLegOddsAsync(context, raceId, legNumber, cancellationToken, entryId, placingAmount);
        var result = odds.FirstOrDefault(x => x.EntryId == entryId);
        if (result is null) throw new InvalidOperationException("The entry does not have valid odds for this leg.");
        return result.CurrentOdds;
    }

    private static decimal CalculateDynamicOdds(decimal baseOdds, decimal entryPool, decimal totalPool, int entryCount)
    {
        if (totalPool <= 0 || entryCount <= 0) return ClampAndRound(baseOdds);
        var averagePool = totalPool / entryCount;
        var pressure = entryPool <= 0 ? 0.5m : entryPool / Math.Max(averagePool, 1m);
        var adjustedOdds = baseOdds / (decimal)Math.Sqrt((double)pressure);
        return ClampAndRound(adjustedOdds);
    }

    private static decimal ClampAndRound(decimal odds)
    {
        return Math.Round(Math.Clamp(odds, MinOdds, MaxOdds), 2, MidpointRounding.AwayFromZero);
    }
}