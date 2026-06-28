namespace Domain.Aggregates.Entities;

public static class PredictionStatus
{
    public const string Pending = "Pending";
    public const string Cancelled = "Cancelled";
    public const string Locked = "Locked";
    public const string Settled = "Settled";
    public const string Won = "Won";
    public const string Lost = "Lost";
}