using Domain.Aggregates.Entities;

namespace Domain.Aggregates.Entities;

public class RaceResult
{
    public int RaceId { get; set; }
    public int EntryId { get; set; }

    public int TotalPoints { get; set; } = 0;
    public int? FinalPosition { get; set; }
    public bool IsRaceDQ { get; set; } = false;
    public int LegWinCount { get; set; } = 0;
    public int LegTop3Count { get; set; } = 0;

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDisqualified { get; set; }

    public string? ViolationNote { get; set; }
    // Navigation
    public Race Race { get; set; } = null!;
    public Entry Entry { get; set; } = null!;

    public ICollection<PrizePointTransaction> PrizePointTransactions { get; set; }
        = new List<PrizePointTransaction>();
}