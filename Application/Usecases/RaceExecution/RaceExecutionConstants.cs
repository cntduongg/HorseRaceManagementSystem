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

    /// <summary>Leg Points: 1st=6, 2nd=5, ... 6th=1, 7th+=0, DNF/DQ=0.</summary>
    public static int LegPointsFor(int? finishPosition, string resultStatus)
    {
        if (resultStatus != ResultFinished || finishPosition is null or < 1)
            return 0;

        return finishPosition.Value switch
        {
            1 => 6,
            2 => 5,
            3 => 4,
            4 => 3,
            5 => 2,
            6 => 1,
            _ => 0
        };
    }

    /// <summary>Prize Points chung cuộc: 1st=1000, 2nd=600, 3rd=400, 4th=200, 5th=100, 6th+=0.</summary>
    public static int PrizePointsFor(int finalPosition)
    {
        return finalPosition switch
        {
            1 => 1000,
            2 => 600,
            3 => 400,
            4 => 200,
            5 => 100,
            _ => 0
        };
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
