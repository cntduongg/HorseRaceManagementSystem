using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.Common;

public sealed record DynamicEntryOdds(
    int EntryId,
    decimal BaseOdds,
    /// <summary>Giá thị trường hiện tại, chưa tính lệnh cược sắp đặt.</summary>
    decimal CurrentOdds,
    /// <summary>
    /// Giá SẼ BỊ KHÓA nếu đặt <c>previewAmount</c> vào chính entry này. Bằng
    /// <see cref="CurrentOdds"/> khi <c>previewAmount</c> ≤ 0.
    /// </summary>
    decimal EffectiveOdds,
    decimal EntryPool,
    decimal TotalPool);

public static class PredictionOddsCalculator
{
    private const decimal MinOdds = 1.10m;
    private const decimal MaxOdds = 25.00m;
    private const string EntryApproved = "Approved";

    /// <summary>
    /// Odds động theo pool cho toàn bộ Entry Approved của race.
    ///
    /// <paramref name="previewAmount"/> là tham số **thuần tính toán** — không ghi DB, không giữ
    /// chỗ trong pool, gọi bao nhiêu lần cũng được. Với mỗi entry, nó trả thêm
    /// <see cref="DynamicEntryOdds.EffectiveOdds"/> = giá sẽ bị khóa NẾU đặt đúng số tiền đó vào
    /// entry ấy. Cần thiết vì lúc ghi lệnh, BE cộng chính số tiền đang đặt vào pool rồi mới tính
    /// giá, nên <c>CurrentOdds × betAmount</c> luôn cao hơn payout thật (race 2 ngựa chưa ai
    /// cược, đặt 50: bảng hiện 2.83x nhưng khóa ở 2.00x → thực nhận 100 chứ không phải ~141).
    /// Preview và ghi lệnh vì thế dùng CHUNG một hàm, không thể lệch nhau.
    /// </summary>
    public static async Task<List<DynamicEntryOdds>> CalculateRaceOddsAsync(
        IApplicationDbContext context,
        int raceId,
        CancellationToken cancellationToken,
        decimal previewAmount = 0m)
    {
        var approvedEntries = await context.Entries
            .AsNoTracking()
            .Where(e => e.RaceId == raceId && e.Status == EntryApproved && e.Odds > 0)
            .Select(e => new { e.EntryId, BaseOdds = e.Odds })
            .ToListAsync(cancellationToken);

        if (approvedEntries.Count == 0) return new List<DynamicEntryOdds>();

        var pools = await context.Predictions
            .AsNoTracking()
            .Where(p => p.RaceId == raceId && p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.FirstEntryId)
            .Select(g => new { EntryId = g.Key, Amount = g.Sum(x => x.BetAmount) })
            .ToDictionaryAsync(x => x.EntryId, x => x.Amount, cancellationToken);

        var totalPool = pools.Values.Sum();
        var preview = previewAmount > 0 ? previewAmount : 0m;

        return approvedEntries
            .Select(entry =>
            {
                pools.TryGetValue(entry.EntryId, out var entryPool);

                var currentOdds = CalculateDynamicOdds(
                    entry.BaseOdds, entryPool, totalPool, approvedEntries.Count);

                // Mô phỏng đúng những gì CreatePrediction làm: tiền vào pool của entry NÀY
                // (và do đó vào cả tổng pool) trước khi tính giá.
                var effectiveOdds = preview <= 0
                    ? currentOdds
                    : CalculateDynamicOdds(
                        entry.BaseOdds, entryPool + preview, totalPool + preview, approvedEntries.Count);

                return new DynamicEntryOdds(
                    entry.EntryId, entry.BaseOdds, currentOdds, effectiveOdds, entryPool, totalPool);
            })
            .ToList();
    }

    /// <summary>
    /// Giá khóa cho một lệnh cược cụ thể — dùng ở đường GHI (<c>CreatePrediction</c>).
    /// Trả về đúng <see cref="DynamicEntryOdds.EffectiveOdds"/> mà preview đã hiện cho spectator.
    /// </summary>
    public static async Task<decimal> CalculateEntryOddsAsync(
        IApplicationDbContext context,
        int raceId,
        int entryId,
        decimal placingAmount,
        CancellationToken cancellationToken)
    {
        var odds = await CalculateRaceOddsAsync(context, raceId, cancellationToken, placingAmount);
        var result = odds.FirstOrDefault(x => x.EntryId == entryId);
        if (result is null) throw new InvalidOperationException("The entry does not have valid odds.");
        return result.EffectiveOdds;
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