namespace HorseRace.DAL.Entities;
public class Race
{
    public int RaceId { get; set; }
    public int TournamentId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime ScheduledStartTime { get; set; }
    public int NumberOfLegs { get; set; } = 3; // 1..10
    public int MaxHorses { get; set; }
    public string RoundType { get; set; } = null!; // Qualifying|Semifinal|Final|Regular
    public string Status { get; set; } = "Scheduled";
    // Scheduled|InProgress|Paused|PendingResult|Finished|Cancelled
    public int? Referee1Id { get; set; }
    public int? Referee2Id { get; set; }
    public DateTime? RegistrationOpenAt { get; set; }
    public DateTime? RegistrationCloseAt { get; set; }
    public DateTime? OddsComputedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public User? Referee1 { get; set; }
    public User? Referee2 { get; set; }
    public ICollection<Leg> Legs { get; set; } = new List<Leg>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<RaceResult> RaceResults { get; set; } = new List<RaceResult>();
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
