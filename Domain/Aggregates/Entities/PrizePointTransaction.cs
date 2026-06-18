using Domain.Aggregates.Enums;

namespace Domain.Aggregates.Entities;

public class PrizePointTransaction
{
    public int PrizePointTransactionId { get; set; }

    public int TournamentId { get; set; }
    public int RaceId { get; set; }
    public int EntryId { get; set; }
    public int UserId { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public int FinalPosition { get; set; }
    public int Points { get; set; }

    public PrizePointTransactionType TransactionType { get; set; }
        = PrizePointTransactionType.Awarded;

    public int? RollbackOfId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // =====================================================
    // NAVIGATION FKs (FIXED PART)
    // =====================================================

    public RaceResult? RaceResult { get; set; }

    public Tournament? Tournament { get; set; }
    public Race? Race { get; set; }
    public Entry? Entry { get; set; }
    public User? User { get; set; }

    public PrizePointTransaction? RollbackOf { get; set; }
    public ICollection<PrizePointTransaction> Rollbacks { get; set; }
        = new List<PrizePointTransaction>();

    // =====================================================
    // IMPORTANT FIX: EXPLICIT FK FOR COMPOSITE KEY
    // =====================================================

    public RaceResult? LinkedRaceResult => null; // optional helper (ignore EF)
}