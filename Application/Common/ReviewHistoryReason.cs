namespace Application.Common;

public static class ReviewHistoryReason
{
    public const int MaxLength = 500;

    public static string? Normalize(
        string? value,
        bool required,
        string fieldName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (required)
                throw new InvalidOperationException($"{fieldName} is required.");

            return null;
        }

        if (normalized.Length > MaxLength)
        {
            throw new InvalidOperationException(
                $"{fieldName} must be at most {MaxLength} characters.");
        }

        return normalized;
    }
}
