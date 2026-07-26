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

    private static string Describe(LegOfficialResult official)
        => official.ResultStatus == RaceExecutionConstants.ResultFinished
            ? $"position {official.FinishPosition?.ToString() ?? "unset"}"
            : official.ResultStatus;
}
