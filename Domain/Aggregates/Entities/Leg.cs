using Domain.Aggregates.Constants;

namespace Domain.Aggregates.Entities;
public class Leg
{
    public int RaceId { get; set; }
    public int LegNumber { get; set; } // 1..10, composite PK
    public string Status { get; set; } = "Pending";
    // Pending|AwaitingSecondReferee|Confirmed|Conflicted|Resolved
    public string? ConfirmationType { get; set; } // AutoMatched | AdminOverride
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ConflictReportedAt { get; set; }
    public string? AdminOverrideReason { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public Race Race { get; set; } = null!;
    public ICollection<LegRefereeEntry> RefereeEntries { get; set; } = new List<LegRefereeEntry>();
    public ICollection<LegOfficialResult> OfficialResults { get; set; } = new List<LegOfficialResult>();
    public ICollection<Violation> Violations { get; set; } = new List<Violation>();
}
