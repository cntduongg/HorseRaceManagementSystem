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

    public static bool IsValid(string status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               All.Contains(status);
    }
}