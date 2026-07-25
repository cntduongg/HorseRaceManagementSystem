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
    private const decimal PoolSmoothingK = 10m; // MinBet — làm mượt pressure, bỏ jump 0.5 đặc biệt
    private const string EntryApproved = "Approved";

    /// <summary>
    /// Odds thị trường của MỌI entry trong một Leg. Đây là nguồn duy nhất cho cả bảng odds
    /// (GetLegPredictionOdds) lẫn odds khóa vào phiếu (CreatePrediction) — nên con số spectator
    /// NHÌN THẤY luôn đúng bằng con số họ NHẬN ĐƯỢC.
    ///
    /// ⚠️ Cố ý KHÔNG nhận "số tiền đang đặt". Trước đây CreatePrediction cộng tiền của chính
    /// người cược vào pool trước khi tính, nên odds khóa luôn thấp hơn bảng đang hiển thị —
    /// người cược đầu tiên vào một entry chịu thiệt nặng nhất. Đừng thêm tham số đó lại.
    /// </summary>
    public static async Task<List<DynamicEntryOdds>> CalculateLegOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int legNumber,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// Odds thị trường của MỘT entry — đúng bằng giá trị entry đó có trong bảng odds.
    /// </summary>
    public static async Task<decimal> CalculateEntryLegOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int legNumber,
        int entryId,
        CancellationToken cancellationToken)
    {
        var odds = await CalculateLegOddsAsync(context, raceId, legNumber, cancellationToken);
        var result = odds.FirstOrDefault(x => x.EntryId == entryId);
        if (result is null) throw new InvalidOperationException("The entry does not have valid odds for this leg.");
        return result.CurrentOdds;
    }

    private static decimal CalculateDynamicOdds(decimal baseOdds, decimal entryPool, decimal totalPool, int entryCount)
    {
        if (totalPool <= 0 || entryCount <= 0) return ClampAndRound(baseOdds);
        var averagePool = totalPool / entryCount;
        var pressure = (entryPool + PoolSmoothingK) / (averagePool + PoolSmoothingK);
        var adjustedOdds = baseOdds / (decimal)Math.Sqrt((double)pressure);
        return ClampAndRound(adjustedOdds);
    }

    private static decimal ClampAndRound(decimal odds)
    {
        return Math.Round(Math.Clamp(odds, MinOdds, MaxOdds), 2, MidpointRounding.AwayFromZero);
    }
}