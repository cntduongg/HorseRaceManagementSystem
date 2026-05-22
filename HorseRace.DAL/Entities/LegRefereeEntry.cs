namespace HorseRace.DAL.Entities;
// APPEND-ONLY — never UPDATE or DELETE
public class LegRefereeEntry
{
    public long LegRefereeEntryId { get; set; }
    public int RaceId { get; set; }
    public int LegNumber { get; set; }
    public int EntryId { get; set; }
    public int RefereeUserId { get; set; }
    public int? FinishPosition { get; set; }
    public string ResultStatus { get; set; } = null!; // Finished | DNF | DQ
    public DateTime SubmittedAt { get; set; }
    public Leg Leg { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User Referee { get; set; } = null!;
}
