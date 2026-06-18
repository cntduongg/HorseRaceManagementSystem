namespace Domain.Aggregates.Entities;

using Domain.Aggregates.Enums;
public class PrizePointTransaction
{
    public int PrizePointTransactionId { get; set; }

    public int TournamentId { get; set; }
    public int RaceId { get; set; }
    public int EntryId { get; set; }
    public int UserId { get; set; }

    // FIX: replace EntityType
    public string SourceType { get; set; } = string.Empty;

    public int FinalPosition { get; set; }
    public int Points { get; set; }

    // FIX: replace string Type
    public PrizePointTransactionType TransactionType { get; set; } = PrizePointTransactionType.Awarded;

    public int? RollbackOfId { get; set; }

    public DateTime CreatedAt { get; set; }

    // FIX: required for update handler
    public DateTime? UpdatedAt { get; set; }

    public RaceResult? RaceResult { get; set; }
    public Tournament? Tournament { get; set; }
    public Race? Race { get; set; }
    public Entry? Entry { get; set; }
    public User? User { get; set; }

    public PrizePointTransaction? RollbackOf { get; set; }
    public ICollection<PrizePointTransaction> Rollbacks { get; set; }
        = new List<PrizePointTransaction>();
}