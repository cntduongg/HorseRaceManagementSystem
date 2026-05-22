namespace HorseRace.DAL.Entities;
public class Violation
{
    public int ViolationId { get; set; }
    public int RaceId { get; set; }
    public int LegNumber { get; set; }
    public int EntryId { get; set; }
    public int ReportedByRefereeId { get; set; }
    public string ViolationType { get; set; } = null!;
    public string? Description { get; set; }
    public string Penalty { get; set; } = null!; // Warning | Demote | DQ
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    public int? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public Leg Leg { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User ReportedByReferee { get; set; } = null!;
}
