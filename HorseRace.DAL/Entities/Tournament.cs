namespace HorseRace.DAL.Entities;
public class Tournament
{
    public int TournamentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "Draft"; // Draft|Open|Ongoing|Finished|Cancelled
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Race> Races { get; set; } = new List<Race>();
}
