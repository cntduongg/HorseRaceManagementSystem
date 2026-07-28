namespace Application.Usecases.RaceExecution;

/// <summary>
/// Hằng số & helper dùng chung cho luồng vận hành đua (Flow 3-5, 8).
/// Position encoding (đồng bộ với FE): >0 = thứ hạng; -1 = DNF; -2 = DQ; null/0 = chưa nhập.
/// </summary>
public static class RaceExecutionConstants
{
    // ── Race status ──
    public const string RaceScheduled = "Scheduled";
    public const string RaceInProgress = "InProgress";
    public const string RacePaused = "Paused";
    public const string RacePendingResult = "PendingResult";
    public const string RaceFinished = "Finished";
    public const string RaceCancelled = "Cancelled";

    // ── Leg status ──
    public const string LegPending = "Pending";
    public const string LegAwaitingSecondReferee = "AwaitingSecondReferee";
    public const string LegConfirmed = "Confirmed";
    public const string LegConflicted = "Conflicted";
    public const string LegResolved = "Resolved";

    // ── Confirmation type ──
    public const string AutoMatched = "AutoMatched";
    public const string AdminOverride = "AdminOverride";

    // ── Result status ──
    public const string ResultFinished = "Finished";
    public const string ResultDnf = "DNF";
    public const string ResultDq = "DQ";

    // ── Entry status ──
    public const string EntryApproved = "Approved";

    // ── Position sentinels ──
    public const int PositionDnf = -1;
    public const int PositionDq = -2;

    /// <summary>
    /// Leg Points TUYẾN TÍNH theo sĩ số: hạng p trong N ngựa → (N - p + 1) điểm.
    /// VD N=8 → 1st=8, 2nd=7 … 8th=1; N=4 → 1st=4 … 4th=1. DNF/DQ = 0.
    /// fieldSize = số Entry Approved của race (không phải cả giải).
    /// Thay cho thang cứng 6/5/4/3/2/1 cũ: race đông ngựa thì hạng chót không còn bị 0 điểm oan.
    /// </summary>
    public static int LegPointsFor(int? finishPosition, string resultStatus, int fieldSize)
    {
        if (resultStatus != ResultFinished || finishPosition is null or < 1)
            return 0;
        if (fieldSize <= 0)
            return 0;

        var position = finishPosition.Value;
        if (position > fieldSize)
            return 0; // phòng thủ: hạng vượt sĩ số là dữ liệu hỏng → không tính điểm

        return fieldSize - position + 1;
    }

    /// <summary>
    /// Đơn giá Prize Points cho mỗi bậc hạng. Neo với thang cũ: race 5 ngựa → nhất được
    /// 5 × 200 = 1000 điểm, đúng bằng mức 1st của thang cứng trước đây.
    /// </summary>
    public const int PrizePointUnit = 200;

    /// <summary>
    /// Prize Points chung cuộc TUYẾN TÍNH theo sĩ số: (N - p + 1) × <see cref="PrizePointUnit"/>.
    /// Cùng nguyên tắc với <see cref="LegPointsFor"/> để hai thang điểm nhất quán.
    /// </summary>
    public static int PrizePointsFor(int finalPosition, int fieldSize)
    {
        if (finalPosition < 1 || fieldSize <= 0 || finalPosition > fieldSize)
            return 0;

        return (fieldSize - finalPosition + 1) * PrizePointUnit;
    }

    /// <summary>
    /// Kiểm tra bộ vị trí do referee/admin nhập cho một leg.
    /// Quy tắc: mỗi giá trị là 1..fieldSize, hoặc -1 (DNF) / -2 (DQ); hạng dương không trùng
    /// và phải liên tục từ 1 (bỏ DNF/DQ ra thì đúng 1..k). Trả null nếu hợp lệ.
    /// </summary>
    public static string? ValidatePositions(IReadOnlyCollection<int> positions, int fieldSize)
    {
        if (fieldSize <= 0)
            return "The race has no approved entries.";

        foreach (var p in positions)
        {
            if (p == PositionDnf || p == PositionDq) continue;
            if (p < 1)
                return "Invalid position value. Use 1..N, -1 (DNF) or -2 (DQ).";
            if (p > fieldSize)
                return $"Position {p} is out of range — this race has {fieldSize} horses (valid positions are 1..{fieldSize}).";
        }

        var ranked = positions.Where(p => p > 0).OrderBy(p => p).ToList();

        if (ranked.Distinct().Count() != ranked.Count)
            return "Duplicate ranking — each position can be assigned to only one entry.";

        for (var i = 0; i < ranked.Count; i++)
        {
            if (ranked[i] != i + 1)
                return $"Rankings must run consecutively from 1 with no gaps (found {ranked[i]} where {i + 1} was expected).";
        }

        return null;
    }

    /// <summary>Chuyển position-code của FE (-1/-2) thành (FinishPosition, ResultStatus).</summary>
    public static (int? finishPosition, string resultStatus) DecodePosition(int position)
    {
        return position switch
        {
            PositionDnf => ((int?)null, ResultDnf),
            PositionDq => ((int?)null, ResultDq),
            > 0 => (position, ResultFinished),
            _ => ((int?)null, ResultDnf) // 0/không hợp lệ coi như DNF
        };
    }

    /// <summary>Chuyển (FinishPosition, ResultStatus) thành position-code cho FE.</summary>
    public static int EncodePosition(int? finishPosition, string resultStatus)
    {
        return resultStatus switch
        {
            ResultDnf => PositionDnf,
            ResultDq => PositionDq,
            _ => finishPosition ?? 0
        };
    }
}
