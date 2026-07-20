namespace Domain.Aggregates.Constants;

public static class LegExecutionStatuses
{
    public const string Pending = "Pending";

    public const string PredictionOpen = "PredictionOpen";

    public const string InProgress = "InProgress";

    public const string AwaitingResult = "AwaitingResult";

    public const string Completed = "Completed";

    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending,
            PredictionOpen,
            InProgress,
            AwaitingResult,
            Completed,
            Cancelled
        };

    // Cửa cược của một Leg đang MỞ khi Leg chưa bắt đầu chạy — tức ∈ {Pending, PredictionOpen,
    // AwaitingResult}. Khóa khi ∈ {InProgress, Completed, Cancelled}. Đây là nguồn chân lý duy
    // nhất cho cả GetRaceLive (bộ chọn leg của Spectator) lẫn odds/place/cancel prediction.
    public static readonly IReadOnlySet<string> BettingOpen =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending,
            PredictionOpen,
            AwaitingResult
        };

    public static bool IsValid(string status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               All.Contains(status);
    }

    public static bool IsBettingOpen(string? status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               BettingOpen.Contains(status);
    }
}