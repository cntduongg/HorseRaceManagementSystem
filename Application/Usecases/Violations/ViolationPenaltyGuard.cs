using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;

namespace Application.Usecases.Violations;

/// <summary>
/// Guard khi áp / gỡ án phạt lên standings (Flow 6).
///
/// Lý do tồn tại: án <c>Demote</c> chỉ có nghĩa khi entry thực sự có **thứ hạng** ở leg đó.
/// Trước đây cả <c>ApproveViolation</c> lẫn <c>UpdateViolation</c> đều bọc phần tụt hạng trong
/// <c>if (official is { ResultStatus: Finished, FinishPosition: not null })</c> mà **không có else**:
/// leg đang DQ/DNF (FinishPosition = null) hoặc chưa có kết quả chính thức thì điều kiện fail,
/// block bị bỏ qua **im lặng** — violation vẫn chuyển sang Approved/Demote nhìn như đã xử lý xong,
/// nhưng kết quả leg không đổi một chữ nào và Admin không có cách nào biết trừ khi tự đối chiếu
/// standings trước/sau. Nay chặn thẳng để Admin chọn lại án phạt (Warning / DQ) một cách có ý thức.
/// </summary>
internal static class ViolationPenaltyGuard
{
    /// <summary>
    /// Kiểm tra án Demote có áp được lên leg này không. Ném <see cref="InvalidOperationException"/>
    /// (→ 400 kèm message) nếu không; trả về bản ghi hợp lệ để caller tụt hạng.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra án Demote đã áp trước đó có gỡ ngược được không (dùng khi Admin sửa violation).
    /// Cùng lý do với <see cref="EnsureDemotable"/>: im lặng bỏ qua sẽ để lại standings sai
    /// mà không ai biết — thà báo lỗi để Admin sửa qua luồng resolve/override.
    /// </summary>
    public static LegOfficialResult EnsureDemoteReversible(LegOfficialResult? official, int legNumber)
    {
        if (official is null)
            throw new InvalidOperationException(
                $"Leg {legNumber} no longer has an official result for this entry, " +
                "so the Demote applied earlier cannot be reverted automatically. " +
                "Adjust the leg result via the resolve/override flow instead of editing the violation.");

        // Đã demote thì vị trí phải >= 2; khác đi nghĩa là kết quả leg đã bị ghi đè
        // (DQ/DNF hoặc override) → trừ ngược 1 hạng sẽ tạo standings sai âm thầm.
        if (official.ResultStatus != RaceExecutionConstants.ResultFinished ||
            official.FinishPosition is not > 1)
            throw new InvalidOperationException(
                $"The leg {legNumber} result for this entry is now " +
                $"{Describe(official)} — the Demote applied earlier cannot be reverted automatically " +
                "(the demoted position has been overwritten). " +
                "Adjust the leg result via the resolve/override flow instead of editing the violation.");

        return official;
    }

    /// <summary>
    /// Áp án <c>Demote</c>: <b>hoán đổi</b> vị trí với entry ngay dưới, rồi tính lại Leg Points
    /// cho <b>cả hai</b>.
    ///
    /// Trước đây chỗ này chỉ làm <c>FinishPosition += 1</c> trên đúng một dòng. Ba hệ quả:
    /// <list type="number">
    ///   <item>Không ai được kéo lên ⇒ leg thủng một vị trí và có hai entry cùng hạng — vi phạm
    ///   đúng cái invariant mà <c>ValidatePositions</c> bắt referee tuân thủ lúc nhập.</item>
    ///   <item>Không có trần: race 2 ngựa, entry hạng 2 bị demote thành hạng 3 → vượt sĩ số →
    ///   <c>LegPointsFor</c> trả 0. Án "tụt một hạng" hoá ra phạt nặng ngang DNF.</item>
    ///   <item>Entry ngay dưới bị vi phạm làm hại nhưng không được bù gì.</item>
    /// </list>
    /// Hoán đổi thì tổng điểm của leg giữ nguyên, tập vị trí vẫn liên tục 1..k, và gỡ ngược
    /// được đối xứng qua <see cref="ReverseDemote"/>.
    /// </summary>
    /// <param name="legResults">TOÀN BỘ kết quả chính thức của leg đó (không chỉ entry vi phạm).</param>
    public static void ApplyDemote(
        IReadOnlyCollection<LegOfficialResult> legResults,
        int entryId,
        int legNumber,
        int fieldSize)
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

        Swap(demoted, promoted, fieldSize);
    }

    /// <summary>
    /// Gỡ án <c>Demote</c> đã áp: hoán đổi ngược với entry ngay <b>trên</b>. Đối xứng hoàn toàn
    /// với <see cref="ApplyDemote"/> — nếu kết quả leg đã bị ghi đè thì báo lỗi thay vì để lại
    /// standings sai âm thầm.
    /// </summary>
    public static void ReverseDemote(
        IReadOnlyCollection<LegOfficialResult> legResults,
        int entryId,
        int legNumber,
        int fieldSize)
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

        Swap(demoted, demotedBack, fieldSize);
    }

    private static void Swap(LegOfficialResult a, LegOfficialResult b, int fieldSize)
    {
        (a.FinishPosition, b.FinishPosition) = (b.FinishPosition, a.FinishPosition);

        a.LegPoints = RaceExecutionConstants.LegPointsFor(a.FinishPosition, a.ResultStatus, fieldSize);
        b.LegPoints = RaceExecutionConstants.LegPointsFor(b.FinishPosition, b.ResultStatus, fieldSize);
    }

    private static string Describe(LegOfficialResult official)
        => official.ResultStatus == RaceExecutionConstants.ResultFinished
            ? $"position {official.FinishPosition?.ToString() ?? "unset"}"
            : official.ResultStatus;
}
