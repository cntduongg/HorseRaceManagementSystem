namespace Domain.Aggregates.Entities;

public class PrizePointTransaction
{
    public Guid PrizePointTransactionId { get; set; }

    public int TournamentId { get; set; }

    public int RaceId { get; set; }

    public int EntryId { get; set; }

    public int UserId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int FinalPosition { get; set; }

    public int Points { get; set; }

    public string Type { get; set; } = "Awarded";

    public Guid? RollbackOfId { get; set; }

    public DateTime CreatedAt { get; set; }

    public RaceResult? RaceResult { get; set; }

    public Tournament? Tournament { get; set; }

    public Race? Race { get; set; }

    public Entry? Entry { get; set; }

    public User? User { get; set; }

    public PrizePointTransaction? RollbackOf { get; set; }

    public ICollection<PrizePointTransaction> Rollbacks { get; set; } = new List<PrizePointTransaction>();
}