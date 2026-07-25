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

    // Cửa cược mở chỉ khi ExecutionStatus = PredictionOpen (sau close-registration).
    // Khóa khi ∈ {Pending, InProgress, AwaitingResult, Completed, Cancelled}.
    // Commit 1b: bỏ AwaitingResult khỏi tập mở (chỉ còn PredictionOpen).
    public static readonly IReadOnlySet<string> BettingOpen =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PredictionOpen
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