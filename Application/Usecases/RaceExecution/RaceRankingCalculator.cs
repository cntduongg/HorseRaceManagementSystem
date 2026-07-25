using Domain.Aggregates.Entities;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Công thức xếp hạng chung cuộc dùng chung cho Flow 8.
/// Dùng bởi cả <see cref="PublishRaceResultCommandHandler"/> (publish thật) và
/// <see cref="Admin.ResultPublication.ReviewRacePublicationQueryHandler"/> (xem trước)
/// để "bảng điểm trước khi publish" luôn khớp với "kết quả publish thật".
///
/// Tie-break (giống spec Flow 8): DQ luôn xuống đáy → tổng Leg Points cao hơn
/// → nhiều Leg về 1st hơn → nhiều Leg về 2nd hơn → vị trí Leg cuối tốt hơn.
/// </summary>
public static class RaceRankingCalculator
{
    /// <summary>Một Entry đã được xếp hạng cùng các chỉ số dùng cho tie-break.</summary>
    public sealed record RankedEntry(
        Entry Entry,
        bool IsDq,
        int TotalPoints,
        int LegWins,
        int Leg2nds,
        int LegTop3,
        int LastLegPos,
        int FinalPosition);

    /// <summary>
    /// Xếp hạng danh sách Entry (đã Approved) theo kết quả Leg chính thức.
    /// </summary>
    /// <param name="approvedEntries">Các Entry trạng thái Approved của race.</param>
    /// <param name="officials">Kết quả Leg chính thức (<see cref="LegOfficialResult"/>) của race.</param>
    /// <param name="raceDqEntryIds">EntryId bị Race DQ (vi phạm đã duyệt, Penalty=DQ) — xếp cuối, 0 điểm.</param>
    public static List<RankedEntry> Rank(
        IReadOnlyCollection<Entry> approvedEntries,
        IReadOnlyCollection<LegOfficialResult> officials,
        ISet<int> raceDqEntryIds)
    {
        var lastLegNumber = officials.Count > 0 ? officials.Max(o => o.LegNumber) : 0;

        return approvedEntries.Select(e =>
            {
                var isDq = raceDqEntryIds.Contains(e.EntryId);
                var rows = officials.Where(o => o.EntryId == e.EntryId).ToList();
                var finished = rows
                    .Where(r => r.ResultStatus == RaceExecutionConstants.ResultFinished)
                    .ToList();
                var lastLeg = rows.FirstOrDefault(r => r.LegNumber == lastLegNumber);
                return new
                {
                    Entry = e,
                    IsDq = isDq,
                    TotalPoints = isDq ? 0 : rows.Sum(r => r.LegPoints),
                    LegWins = isDq ? 0 : finished.Count(r => r.FinishPosition == 1),
                    Leg2nds = isDq ? 0 : finished.Count(r => r.FinishPosition == 2),
                    LegTop3 = isDq ? 0 : finished.Count(r => r.FinishPosition is >= 1 and <= 3),
                    LastLegPos = (!isDq && lastLeg is
                        { ResultStatus: RaceExecutionConstants.ResultFinished, FinishPosition: not null })
                        ? lastLeg.FinishPosition!.Value
                        : int.MaxValue
                };
            })
            .OrderBy(x => x.IsDq)
            .ThenByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.LegWins)
            .ThenByDescending(x => x.Leg2nds)
            .ThenBy(x => x.LastLegPos)
            // Deterministic final tie-break so preview (publication-review) matches publish
            // and prize points are not decided by undefined DB row order.
            .ThenBy(x => x.Entry.EntryId)
            .Select((x, i) => new RankedEntry(
                x.Entry,
                x.IsDq,
                x.TotalPoints,
                x.LegWins,
                x.Leg2nds,
                x.LegTop3,
                x.LastLegPos,
                i + 1))
            .ToList();
    }
}
