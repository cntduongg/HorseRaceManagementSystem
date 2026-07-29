namespace Domain.Aggregates.Entities;
// Composite PK: (RaceId, LegNumber, EntryId)
public class LegOfficialResult
{
    public int RaceId { get; set; }
    public int LegNumber { get; set; }
    public int EntryId { get; set; }
    public int? FinishPosition { get; set; }
    public string ResultStatus { get; set; } = null!; // Finished | DNF | DQ
    public decimal LegPoints { get; set; } = 0m;
    public string ConfirmationType { get; set; } = null!; // AutoMatched | AdminOverride
    public DateTime ConfirmedAt { get; set; }
    public int? ConfirmedByAdminId { get; set; }
    public string? OverrideReason { get; set; }
    public Leg Leg { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
}