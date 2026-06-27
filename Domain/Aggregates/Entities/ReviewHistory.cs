namespace Domain.Aggregates.Entities;

using Domain.Aggregates.Enums;
public class ReviewHistory
{
    public long Id { get; set; }

    public ReviewEntity EntityType { get; set; }

    public int EntityId { get; set; }

    public ReviewAction Action { get; set; }

    public string? Reason { get; set; }

    public int AdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Admin { get; set; } = null!;
}