namespace HorseRace.DAL.Entities;
public class Horse
{
    public int HorseId { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string? Breed { get; set; }
    public int? BirthYear { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    public string? RejectionReason { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<JockeyInvitation> Invitations { get; set; } = new List<JockeyInvitation>();
}
