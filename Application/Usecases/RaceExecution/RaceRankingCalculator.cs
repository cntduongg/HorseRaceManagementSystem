using Domain.Aggregates.Entities;

namespace Application.Usecases.RaceExecution;

public static class RaceRankingCalculator
{
    public sealed record RankedEntry(
        Entry Entry,
        bool IsDq,
        decimal TotalPoints,
        int LegWins,
        int Leg2nds,
        int LegTop3,
        int LastLegPos,
        int FinalPosition);

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
                TotalPoints = isDq ? 0m : rows.Sum(r => r.LegPoints),
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