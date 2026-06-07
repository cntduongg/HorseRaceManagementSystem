namespace Domain.Aggregates.Entities;

public class JockeyProfile
{
    public Guid UserId { get; set; }

    public string? LicenseNumber { get; set; }

    public decimal? Weight { get; set; }

    public string? Bio { get; set; }

    public int TotalRaces { get; set; }

    public int TotalWins { get; set; }

    public int TotalTop3 { get; set; }

    public int CareerPrizePoints { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}