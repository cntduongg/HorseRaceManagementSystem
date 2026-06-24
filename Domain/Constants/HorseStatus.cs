namespace Domain.Constants;

/// <summary>
/// Lifecycle states of a <see cref="Aggregates.Entities.Horse"/> (Flow 1).
/// Pending → Approved (admin approves) | Rejected (admin rejects).
/// An Approved horse can later be Revoked by an admin.
/// Only Approved horses are eligible to join Race Entries (Flow 2).
/// </summary>
public static class HorseStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
}
