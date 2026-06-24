namespace Domain.Constants;

/// <summary>
/// Lifecycle states of a race <see cref="Aggregates.Entities.Entry"/> (Flow 2).
/// Withdrawn also covers entries auto-cancelled when their horse is revoked (Flow 1).
/// </summary>
public static class EntryStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";
}
