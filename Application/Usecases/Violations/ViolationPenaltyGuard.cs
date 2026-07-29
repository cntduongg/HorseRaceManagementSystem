using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;

namespace Application.Usecases.Violations;

internal static class ViolationPenaltyGuard
{
    public static LegOfficialResult EnsureDemotable(LegOfficialResult? official, int legNumber)
    {
        if (official is null)
            throw new InvalidOperationException(
                $"Leg {legNumber} has no confirmed official result for this entry yet, " +
                "so a Demote penalty would have no effect. " +
                "Wait until the leg is confirmed (or resolved), then approve this violation.");

        if (official.ResultStatus != RaceExecutionConstants.ResultFinished)
            throw new InvalidOperationException(
                $"This entry is already recorded as {official.ResultStatus} in leg {legNumber}, " +
                "so there is no finishing position to demote. " +
                "Use Warning or DQ instead, or fix the leg result via the resolve/override flow first.");

        if (official.FinishPosition is null)
            throw new InvalidOperationException(
                $"This entry has no finishing position recorded in leg {legNumber}, " +
                "so a Demote penalty would have no effect. " +
                "Fix the leg result via the resolve/override flow first.");

        return official;
    }

    public static LegOfficialResult EnsureDemoteReversible(LegOfficialResult? official, int legNumber)
    {
        if (official is null)
            throw new InvalidOperationException(
                $"Leg {legNumber} no longer has an official result for this entry, " +
                "so the Demote applied earlier cannot be reverted automatically. " +
                "Adjust the leg result via the resolve/override flow instead of editing the violation.");

        if (official.ResultStatus != RaceExecutionConstants.ResultFinished ||
            official.FinishPosition is not > 1)
            throw new InvalidOperationException(
                $"The leg {legNumber} result for this entry is now " +
                $"{Describe(official)} — the Demote applied earlier cannot be reverted automatically " +
                "(the demoted position has been overwritten). " +
                "Adjust the leg result via the resolve/override flow instead of editing the violation.");

        return official;
    }

    /// <param name="legResults">TOÀN BỘ kết quả chính thức của leg đó (không chỉ entry vi phạm).</param>
    /// <param name="totalLegs">Tổng số leg của race — để Swap biết leg này có phải leg cuối (bonus) không.</param>
    public static void ApplyDemote(
        IReadOnlyCollection<LegOfficialResult> legResults,
        int entryId,
        int legNumber,
        int fieldSize,
        int totalLegs)
    {
        var demoted = EnsureDemotable(
            legResults.FirstOrDefault(o => o.EntryId == entryId),
            legNumber);

        var targetPosition = demoted.FinishPosition!.Value + 1;

        var promoted = legResults.FirstOrDefault(o =>
            o.EntryId != entryId &&
            o.ResultStatus == RaceExecutionConstants.ResultFinished &&
            o.FinishPosition == targetPosition)
            ?? throw new InvalidOperationException(
                $"This entry finished last ({demoted.FinishPosition}) in leg {legNumber} — " +
                "there is nobody below to swap with, so a Demote penalty cannot be applied. " +
                "Use Warning or DQ instead.");

        Swap(demoted, promoted, fieldSize, legNumber, totalLegs);
    }

    public static void ReverseDemote(
        IReadOnlyCollection<LegOfficialResult> legResults,
        int entryId,
        int legNumber,
        int fieldSize,
        int totalLegs)
    {
        var demoted = EnsureDemoteReversible(
            legResults.FirstOrDefault(o => o.EntryId == entryId),
            legNumber);

        var targetPosition = demoted.FinishPosition!.Value - 1;

        var demotedBack = legResults.FirstOrDefault(o =>
            o.EntryId != entryId &&
            o.ResultStatus == RaceExecutionConstants.ResultFinished &&
            o.FinishPosition == targetPosition)
            ?? throw new InvalidOperationException(
                $"Leg {legNumber} no longer has an entry at position {targetPosition}, " +
                "so the Demote applied earlier cannot be swapped back automatically. " +
                "Adjust the leg result via the resolve/override flow instead of editing the violation.");

        Swap(demoted, demotedBack, fieldSize, legNumber, totalLegs);
    }

    private static void Swap(
        LegOfficialResult a, LegOfficialResult b, int fieldSize, int legNumber, int totalLegs)
    {
        (a.FinishPosition, b.FinishPosition) = (b.FinishPosition, a.FinishPosition);

        a.LegPoints = RaceExecutionConstants.LegPointsFor(
            a.FinishPosition, a.ResultStatus, fieldSize, legNumber, totalLegs);
        b.LegPoints = RaceExecutionConstants.LegPointsFor(
            b.FinishPosition, b.ResultStatus, fieldSize, legNumber, totalLegs);
    }

    private static string Describe(LegOfficialResult official)
        => official.ResultStatus == RaceExecutionConstants.ResultFinished
            ? $"position {official.FinishPosition?.ToString() ?? "unset"}"
            : official.ResultStatus;
}